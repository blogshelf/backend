using System.Numerics;
using Aliyun.OTS;
using Aliyun.OTS.Request;
using Ardalis.GuardClauses;
using backend.Utils;
using MessagePack;

namespace backend.models;
/* Misc */
[MessagePackObject]
public sealed record Pong
{
    [Key("status")] public string? Status { get; set; } = "System.State.Online";

    [Key("request_id")] public string RequestId { get; set; } = IdGen.New();
}

[MessagePackObject]
public sealed record DbConnStat
{
    [Key("status")] public string Status { get; set; } = "Database.Conn.Unavailable";
    [Key("request_id")] public string RequestId { get; set; } = IdGen.New();
    [Key("data")] public TestDbConnData? Data { get; set; } = new();
}

[MessagePackObject]
public sealed record TestDbConnData
{
    [Key("error")] public string? Error { get; set; }
    [Key("tables")] public List<string> Tables { get; set; } = new();

    public TestDbConnData GetTables(OTSClient client)
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
/* Misc */

/* Auth */
/* SRP */
[MessagePackObject]
public sealed record SrpPublic
{
    [Key("status")] public string Status { get; set; } = "Auth.Srp.PublicData.Unavailable";
    [Key("request_id")] public string RequestId { get; set; } = IdGen.New();
    [Key("data")] public SrpPublicData Data { get; set; } = new();
}
[MessagePackObject]
public sealed record SrpPublicData
{
    [Key("N")] public byte[] N { get; set; } = Tools.Pad(Params.N);
    [Key("G")] public BigInteger G { get; set; } = Params.G;
    [Key("length")] public int Length { get; set; } = Params.Length;
    [Key("hash")] public byte[] Hash { get; set; } = [.. Params.Hash];
}
/* SRP */
/* Auth */