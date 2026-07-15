using System.Net;
using Aliyun.OTS;
using backend.database;
using backend.initialization;
using backend.middleware;
using backend.models;
using backend.resources;
using backend.Utils;
using MessagePack;
using Microsoft.AspNetCore.Mvc;
using Tea.Utils;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace backend.handler;

public static class SrpEndpoints
{
    public static void MapSrpEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth/srp");

        group.MapGet("/config", GetConfig);
        group.MapMethods("/config", ["OPTIONS"], GetConfig);
        group.MapPost("/register", Register).WithMetadata(new ReqSignAttr());
        group.MapMethods("/register", ["OPTIONS"], GetConfig);
    }

    private static IResult GetConfig()
    {
        var resp = new SrpPublic
        {
            Data = new SrpPublicData()
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

    private static async Task<IResult> Register(OTSClient client, HttpContext ctx, DotEnv env)
    {
        using var ms = new MemoryStream();
        await ctx.Request.Body.CopyToAsync(ms);
        var body = MessagePackSerializer.Deserialize<RegisterSrp>(ms.ToArray());
        var clientIp = GetClientIp(ctx);
        var userId = UserId.Generate(DateTimeOffset.UtcNow, clientIp, env.EcdhPublicKey);
        try
        {
            new Methods(client).Register(body, ctx.Items["SignatureVerified"].ToSafeBool(), userId);
        }
        catch (Exception e)
        {
            switch (e)
            {
                case InvalidOperationException:
                    return Http.MsgPack(new SrpRegister
                    {
                        Status = lanuage.Request_Error_RejectOperation,
                        Data = null
                    }, Status409Conflict);
                case SystemException:
                    return Http.MsgPack(new SrpRegister
                    {
                        Status = lanuage.Database_Error_DataConsistency,
                        Data = null
                    }, Status500InternalServerError);
                default:
                    return Http.MsgPack(new SrpRegister
                    {
                        Status = lanuage.Internal_Server_Error,
                        Data = null
                    }, Status500InternalServerError);
            }
        }
        return Http.MsgPack(new SrpRegister
        {
            Status = "Action.Status.Ok",
            Data = new SrpRegisterData
            {
                UserId = userId
            }
        });
    }
}
