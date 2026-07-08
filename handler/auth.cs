using backend.models;
using MessagePack;

namespace backend.handler;

using static MessagePackSerializer;

public static class SrpEndpoints
{
    public static void MapSrpEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth/srp");

        group.MapGet("/config", GetConfig);
    }

    private static IResult GetConfig()
    {
        var resp = new SrpPublic
        {
            Data = new SrpPublicData()
        };
        return Results.Bytes(Serialize(resp));
    }
}