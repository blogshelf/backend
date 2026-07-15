using Aliyun.OTS;
using backend.models;
using backend.resources;
using backend.Utils;

namespace backend.handler;

public static class MiscEndpoints
{
    public static void MapMiscEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/misc");

        group.MapGet("/ping", Ping);
        group.MapGet("/health/db", CheckDb);
    }

    private static IResult Ping(HttpContext ctx) => Http.MsgPack(new Response(ctx.GetRequestId())
    {
        Status = lanuage.System_Sttatus_Online,
    });

    private static IResult CheckDb(OTSClient client, HttpContext ctx)
    {
        var data = new TestDbConn().GetTables(client);
        return Http.MsgPack(new Response<TestDbConn>(ctx.GetRequestId())
        {
            Status = data.Error is null ? lanuage.Database_Conn_Online : lanuage.Database_Conn_Unavailable,
            Data = data,
        });
    }
}