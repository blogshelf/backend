using System.Net;
using System.Security.Cryptography;
using Aliyun.OTS;
using backend.database;
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
        group.MapPost("/register", Register).WithMetadata(new ReqSignAttr()).AddEndpointFilter<MsgPackBodyFilter<RegisterSrp>>();
        group.MapPost("/login/begin", LoginBegin).AddEndpointFilter<MsgPackBodyFilter<LoginSrp>>();
    }

    private static IResult GetConfig(HttpContext ctx)
    {
        var resp = new Response<SrpPublic>(ctx.GetRequestId())
        {
            Status = lanuage.Auth_Srp_PublicData_OK,
            Data = new SrpPublic(),
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
        try
        {
            new Methods(client).Register(body,
                ctx.Items["SignatureVerified"].ToSafeBool(),
                UserId.Generate(DateTimeOffset.UtcNow, GetClientIp(ctx), env.EcdhPublicKey));
        }
        catch (Exception e)
        {
            Logger.LogError(e, lanuage.Log_Error_FailedAt, "Register");
            switch (e)
            {
                case InvalidOperationException:
                    return Http.MsgPack(new Response<SrpRegister>(ctx.GetRequestId())
                    {
                        Status = lanuage.Request_Error_RejectOperation,
                        Data = null
                    }, Status409Conflict);
                case SystemException:
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
                UserId = UserId.Generate(DateTimeOffset.UtcNow, GetClientIp(ctx), env.EcdhPublicKey)
            }
        });
    }

    private static IResult LoginBegin(OTSClient client, HttpContext ctx, OtpService otp)
    {
        var body = ctx.GetBody<LoginSrp>();
        try
        {
            var userIdentity = new UserIdentity
            {
                IdentityType = "srp",
                IdentityKey = body.Username
            };
            _ = userIdentity.ReadAuthData<SrpAuthData>(client);

            User? user = null;
            for (var perm = 0; perm <= 1; perm++)
            {
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
                catch (IOException) { }
            }
            if (user is null)
                throw new IOException("Database.User.NotFound");

            var methods = new Methods(client);
            var result = methods.ServerProof(body, user, otp);

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            new Session
            {
                UserId = userIdentity.UserId,
                SessionId = token,
                SrpState = false,
                SrpB = result.B,
                SrpA = body.A,
                SrpServerSecret = result.ServerSecret,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 120_000,
            }.Create(client);

            return Http.MsgPack(new Response<SrpChallenge>(ctx.GetRequestId())
            {
                Status = lanuage.Action_Status_Ok,
                Data = new SrpChallenge
                {
                    Salt = result.Salt,
                    B = result.B,
                    Token = token,
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
                    ArgumentException ae => ae.Message,
                    InvalidOperationException ioe => ioe.Message,
                    _ => lanuage.Internal_Server_Error,
                }
            }, e switch
            {
                ArgumentException => Status400BadRequest,
                _ => Status500InternalServerError,
            });
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
            return Http.MsgPack(new Response(ctx.GetRequestId()));
        }
        try
        {
            await es.SendAsync(body.Mail, lanuage.Mail_VerifyAccount_Title, EmailTemplates.OtpVerification(otp));
        }
        catch (Exception e)
        {
            Logger.LogError(e,lanuage.Log_Error_FailedAt, "SendOtpMail");
            return Http.MsgPack(new Response(ctx.GetRequestId())
            {
                Status = lanuage.Mail_VerifyAccount_SendFail
            },statusCode:Status500InternalServerError);
        }
        return Http.MsgPack(new Response(ctx.GetRequestId())
        {
            Status = lanuage.Action_Status_Ok,
        });
    }
}
