﻿using Aliyun.OTS;
using Aliyun.OTS.Request;
using Ardalis.GuardClauses;
using backend.Utils;
using MessagePack;

namespace backend.models;

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