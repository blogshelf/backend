using Aliyun.OTS;
using backend.models;
using backend.Utils;
using static MessagePack.MessagePackSerializer;
using static Microsoft.AspNetCore.Http.Results;

namespace backend.handler;

public static class MiscEndpoints
{
    public static void MapMiscEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/misc");

        group.MapGet("/ping", Ping);
        group.MapMethods("/ping", ["OPTIONS"], Ping);

        group.MapGet("/health/db", CheckDb);
        group.MapMethods("/health/db", ["OPTIONS"], CheckDb);
    }

    private static IResult Ping()
    {
        StatusCode(StatusCodes.Status200OK);
        return Bytes(Serialize(new Pong()), "application/vnd.msgpack");
    }

    private static IResult CheckDb(OTSClient client)
    {
        var data = new TestDbConnData().GetTables(client);

        var resp = new DbConnStat
        {
            RequestId = IdGen.New(),
            Status = data.Error is null ? "Database.Conn.Online" : "Database.Conn.Unavailable",
            Data = data,
        };

        return Bytes(Serialize(resp), "application/vnd.msgpack");
    }
}