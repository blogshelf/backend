using System.Numerics;
using Aliyun.OTS;
using Aliyun.OTS.Request;
using Ardalis.GuardClauses;
using backend.initialization;
using backend.resources;
using backend.Utils;
using MessagePack;

namespace backend.models;

[MessagePackObject]
public sealed record Response
{
    public Response(string? requestId = null)
    {
        RequestId = requestId ?? IdGen.New();
    }
    [Key("status")] public string Status { get; set; } = lanuage.Response_Error_NoInitialized;
    [Key("request_id")] public string RequestId { get; }
}

[MessagePackObject]
public sealed record Response<T>
{
    public Response(string? requestId = null)
    {
        RequestId = requestId ?? IdGen.New();
    }

    [Key("status")] public string Status { get; set; } = lanuage.Response_Error_NoInitialized;
    [Key("request_id")] public string RequestId { get; set; }
    [Key("data")] public T? Data { get; set; }
}

/* Data */
[MessagePackObject]
public sealed record TestDbConn
{
    [Key("error")] public string? Error { get; set; }
    [Key("tables")] public List<string> Tables { get; set; } = new();

    public TestDbConn GetTables(OTSClient client)
    {
        try
        {
            var resp = client.ListTable(new ListTableRequest());
            Tables = resp.TableNames.ToList();
            Guard.Against.NullOrEmpty(resp.TableNames);
        }
        catch (Exception e)
        {
            Error = e.Message;
            Tables = new List<string>();
            return this;
        }

        return this;
    }
}

[MessagePackObject]
public sealed record SrpPublic
{
    [Key("N")] public byte[] N { get; set; } = Tools.Pad(Params.N);
    [Key("G")] public BigInteger G { get; set; } = Params.G;
    [Key("length")] public int Length { get; set; } = Params.Length;
    [Key("hash")] public byte[] Hash { get; set; } = [.. Params.Hash];
}

[MessagePackObject]
public sealed record SrpRegister
{
    [Key("user_id")] public byte[] UserId { get; set; } = [];
}

[MessagePackObject]
public sealed record SrpChallenge
{
    [Key("salt")] public required byte[] Salt { get; init; }
    [Key("B")] public required byte[] B { get; init; }
    [Key("token")] public required string Token { get; init; }
}
/* Data */
