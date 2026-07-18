using System.Net;
using System.Security.Cryptography;
using Aliyun.OTS;
using backend.database;
using backend.exceptions;
using backend.initialization;
using backend.middleware;
using backend.models;
using backend.resources;
using backend.services;
using backend.Utils;
using Tea.Utils;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace backend.handler;

public static class SrpEndpoints
{
    private static readonly ILogger Logger = initialization.Logger.LogFactory.CreateLogger("Handler.Auth");

    public static void MapSrpEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth/srp");

        group.MapGet("/config", GetConfig);
        group.MapPost("/register", Register).WithMetadata(new ReqSignAttr())
            .AddEndpointFilter<MsgPackBodyFilter<RegisterSrp>>();
        group.MapPost("/login/begin", LoginBegin).AddEndpointFilter<MsgPackBodyFilter<LoginSrpStart>>();
        group.MapPost("/login/complete", LoginCompete).AddEndpointFilter<MsgPackBodyFilter<LoginSrpComplete>>();
        group.MapPost("/seed-renew", RenewSeed).RequireAuthorization("Session");
    }

    private static IResult GetConfig(HttpContext ctx)
    {
        var resp = new Response<SrpPublic>(ctx.GetRequestId())
        {
            Status = lanuage.Auth_Srp_PublicData_OK,
            Data = new SrpPublic()
        };
        return Http.MsgPack(resp);
    }

    private static byte[] GetClientIp(HttpContext ctx)
    {
        if (ctx.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded)
            && IPAddress.TryParse(forwarded.FirstOrDefault(), out var ip))
            return ip.GetAddressBytes();

        return ctx.Connection.RemoteIpAddress?.GetAddressBytes() ?? [];
    }

    private static IResult Register(OTSClient client, HttpContext ctx, DotEnv env)
    {
        var body = ctx.GetBody<RegisterSrp>();
        var userId = UserId.Generate(DateTimeOffset.UtcNow, GetClientIp(ctx), env.EcdhPublicKey);
        try
        {
            new Methods(client).Register(body,
                ctx.Items["SignatureVerified"].ToSafeBool(),
                userId);
        }
        catch (Exception e)
        {
            Logger.LogError(e, lanuage.Log_Error_FailedAt, "Register");
            switch (e)
            {
                case ConflictException:
                    return Http.MsgPack(new Response<SrpRegister>(ctx.GetRequestId())
                    {
                        Status = lanuage.Request_Error_RejectOperation,
                        Data = null
                    }, Status409Conflict);
                case ValidationException ve:
                    return Http.MsgPack(new Response<SrpRegister>(ctx.GetRequestId())
                    {
                        Status = ve.Message,
                        Data = null
                    }, Status400BadRequest);
                case IOException:
                    return Http.MsgPack(new Response<SrpRegister>(ctx.GetRequestId())
                    {
                        Status = lanuage.Database_Error_DataConsistency,
                        Data = null
                    }, Status500InternalServerError);
                default:
                    return Http.MsgPack(new Response<SrpRegister>(ctx.GetRequestId())
                    {
                        Status = lanuage.Internal_Server_Error,
                        Data = null
                    }, Status500InternalServerError);
            }
        }

        return Http.MsgPack(new Response<SrpRegister>(ctx.GetRequestId())
        {
            Status = lanuage.Action_Status_Ok,
            Data = new SrpRegister
            {
                UserId = userId
            }
        });
    }

    private static IResult LoginBegin(OTSClient client, HttpContext ctx, OtpService otp)
    {
        var body = ctx.GetBody<LoginSrpStart>();
        try
        {
            var userIdentity = new UserIdentity
            {
                IdentityType = "srp",
                IdentityKey = body.Username
            };
            var test = userIdentity.ReadAuthData<SrpAuthData>(client);
#if DEBUG
            Logger.LogDebug($"{test}");
#endif
            User? user = null;
            for (var perm = 0; perm <= 1; perm++)
                try
                {
                    user = new User
                    {
                        UserId = userIdentity.UserId,
                        PermissionCode = perm,
                        State = 0
                    }.GetSingle(client);
                    break;
                }
                catch (NotFoundException)
                {
                }

            if (user is null)
                throw new NotFoundException("Database.User.NotFound");

            var methods = new Methods(client);
            var result = methods.BeginSrpLogin(body, user, otp);

            var token = RandomNumberGenerator.GetBytes(32);
            new Session
            {
                UserId = userIdentity.UserId,
                SessionId = token,
                SrpState = false,
                SrpB = result.B,
                SrpA = body.A,
                SrpServerSecret = result.ServerSecret,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 120_000
            }.Create(client);

            return Http.MsgPack(new Response<SrpChallenge>(ctx.GetRequestId())
            {
                Status = lanuage.Action_Status_Ok,
                Data = new SrpChallenge
                {
                    Salt = result.Salt,
                    B = result.B,
                    Token = token
                }
            });
        }
        catch (Exception e)
        {
            Logger.LogError(e, lanuage.Log_Error_FailedAt, "SRP.Login");
            return Http.MsgPack(new Response(ctx.GetRequestId())
            {
                Status = e switch
                {
                    ValidationException ve => ve.Message,
                    NotFoundException nf => nf.Message,
                    _ => lanuage.Internal_Server_Error
                }
            }, e switch
            {
                ValidationException => Status400BadRequest,
                NotFoundException => Status404NotFound,
                _ => Status500InternalServerError
            });
        }
    }

    private static IResult LoginCompete(OTSClient client, HttpContext ctx)
    {
        var body = ctx.GetBody<LoginSrpComplete>();
        try
        {
            var session = new Session
            {
                UserId = body.UserId,
                SessionId = body.Token
            }.GetSingle(client);
            User? user = null;
            for (var perm = 0; perm <= 1; perm++)
                try
                {
                    user = new User
                    {
                        UserId = body.UserId,
                        PermissionCode = perm,
                        State = 0
                    }.GetSingle(client);
                    break;
                }
                catch (NotFoundException e)
                {
#if DEBUG
                    Logger.LogWarning(e.StackTrace);
#endif
                    Logger.LogWarning(lanuage.Auth_Login_NotFound);
                    Logger.LogWarning(e.Message);
                }

            if (user is null) throw new NotFoundException("Database.User.NotFound");

            var userIdentity = new UserIdentity
            {
                IdentityKey = user.Name,
                IdentityType = "srp",
                UserId = body.UserId
            };
            userIdentity.ReadAuthData<SrpAuthData>(client);
            return Http.MsgPack(new Response<Tools.LoginCompleteReturn>(ctx.GetRequestId())
            {
                Status = lanuage.Action_Status_Ok,
                Data = new Methods(client).CompleteSrpReturn(body, user, userIdentity, session)
            });
        }
        catch (Exception e)
        {
            Logger.LogError(e, lanuage.Log_Error_FailedAt, "Srp.LoginComplete");
            return Http.MsgPack(new Response(ctx.GetRequestId())
            {
                Status = e switch
                {
                    ValidationException ve => ve.Message,
                    NotFoundException nf => nf.Message,
                    ConflictException ce => ce.Message,
                    _ => lanuage.Internal_Server_Error
                }
            }, e switch
            {
                ValidationException => Status400BadRequest,
                NotFoundException => Status404NotFound,
                ConflictException => Status409Conflict,
                _ => Status500InternalServerError
            });
        }
    }

    private static IResult RenewSeed(HttpContext ctx, OTSClient client)
    {
        try
        {
            var userId = Convert.FromBase64String(ctx.User.FindFirst("userId")!.Value);
            var sessionId = Convert.FromBase64String(ctx.User.FindFirst("sessionId")!.Value);

            var session = new Session
            {
                UserId = userId,
                SessionId = sessionId
            }.GetSingle(client);

            if (session.Seed.Length == 0)
                return Http.MsgPack(new Response(ctx.GetRequestId())
                {
                    Status = "Session.NoSeed"
                }, Status400BadRequest);

            var oldSeed = session.Seed;
            var newSeed = RandomNumberGenerator.GetBytes(32);
            session.UpdateSeed(client, oldSeed, newSeed);

            var renewalKey = RequestTokenHelper.DeriveRenewalKey(oldSeed);
            var encryptedNewSeed = RequestTokenHelper.CreateToken(renewalKey, newSeed);

            return Http.MsgPack(new Response<SeedRenewal>(ctx.GetRequestId())
            {
                Status = lanuage.Action_Status_Ok,
                Data = new SeedRenewal { EncryptedSeed = encryptedNewSeed }
            });
        }
        catch (Exception e)
        {
            Logger.LogError(e, lanuage.Log_Error_FailedAt, "SeedRenew");
            return Http.MsgPack(new Response(ctx.GetRequestId())
            {
                Status = lanuage.Internal_Server_Error
            }, Status500InternalServerError);
        }
    }
}

public static class VerifyEndpoints
{
    private static readonly ILogger Logger = initialization.Logger.LogFactory.CreateLogger("Handler.Verify");

    public static void MapVerifyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth/verify");
        group.MapPut("/mail/send", GetOtpMail).AddEndpointFilter<MsgPackBodyFilter<VerifyForMail>>();
    }

    private static async Task<IResult> GetOtpMail(OtpService otpService, HttpContext ctx, IEmailSender es)
    {
        var body = ctx.GetBody<VerifyForMail>();
        if (otpService.IsRateLimited(body.Mail))
            return Http.MsgPack(new Response(ctx.GetRequestId())
            {
                Status = lanuage.Mail_VerifyAccount_SendRatelimited
            }, Status429TooManyRequests);
        var otp = otpService.Generate();
        try
        {
            otpService.Store(body.Mail, body.UserId, otp);
        }
        catch (Exception e)
        {
            Logger.LogError(e, lanuage.Log_Error_FailedAt, "StoreOTP");
            return Http.MsgPack(new Response(ctx.GetRequestId())
            {
                Status = lanuage.Internal_Server_Error
            }, Status500InternalServerError);
        }

        try
        {
            await es.SendAsync(body.Mail, lanuage.Mail_VerifyAccount_Title, EmailTemplates.OtpVerification(otp));
        }
        catch (Exception e)
        {
            Logger.LogError(e, lanuage.Log_Error_FailedAt, "SendOtpMail");
            return Http.MsgPack(new Response(ctx.GetRequestId())
            {
                Status = lanuage.Mail_VerifyAccount_SendFail
            }, Status500InternalServerError);
        }

        return Http.MsgPack(new Response(ctx.GetRequestId())
        {
            Status = lanuage.Action_Status_Ok
        });
    }
}