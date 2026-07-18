using Aliyun.OTS;
using Aliyun.OTS.DataModel;
using Aliyun.OTS.DataModel.ConditionalUpdate;
using Aliyun.OTS.Request;
using backend.exceptions;
using MessagePack;
using Tea.Utils;
using Condition = Aliyun.OTS.DataModel.Condition;
using DeleteRowRequest = Aliyun.OTS.Request.DeleteRowRequest;
using GetRowRequest = Aliyun.OTS.Request.GetRowRequest;
using PutRowRequest = Aliyun.OTS.Request.PutRowRequest;
using RowExistenceExpectation = Aliyun.OTS.DataModel.RowExistenceExpectation;
using UpdateRowRequest = Aliyun.OTS.Request.UpdateRowRequest;
using UpdateRowResponse = Aliyun.OTS.Response.UpdateRowResponse;

namespace backend.database;

using static MessagePackSerializer;

/* Post */
public class Post
{
    private static readonly string TableName = "Post" +
                                               Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") switch
                                               {
                                                   "Development" => "_develop",
                                                   "Staging" => $"_pr_{Environment.GetEnvironmentVariable("HEAD")}",
                                                   _ => ""
                                               };

    public string PostId { get; set; } = "";
    public int MonthId { get; set; }
    public byte[] Sha512 { get; set; } = [];
    public string ObjectKey { get; set; } = "";
    public int ObjectSize { get; set; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public byte[] Tags { get; set; } = [];
    public bool IsPrivate { get; set; }
    public int State { get; set; }
    public byte[] AccessPolicy { get; set; } = [];
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
    public int Version { get; set; }
}
/* Post */

/* Comment */
public class Comment
{
    private static readonly string TableName = "Comment" +
                                               Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") switch
                                               {
                                                   "Development" => "_develop",
                                                   "Staging" => $"_pr_{Environment.GetEnvironmentVariable("HEAD")}",
                                                   _ => ""
                                               };

    public string PostId { get; set; } = "";
    public string CommentId { get; set; } = "";
    public string? ParentCommentId { get; set; }
    public byte[] UserId { get; set; } = [];
    public string Body { get; set; } = "";
    public int State { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
    public byte[] UserSignature { get; set; } = [];
    public string ClientKeyId { get; set; } = "";
}
/* Comment */

/* CommentLike */
public class CommentLike
{
    private static readonly string TableName = "CommentLike" +
                                               Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") switch
                                               {
                                                   "Development" => "_develop",
                                                   "Staging" => $"_pr_{Environment.GetEnvironmentVariable("HEAD")}",
                                                   _ => ""
                                               };

    public string CommentId { get; set; } = "";
    public byte[] UserId { get; set; } = [];
}
/* CommentLike */

/* User */
public class User
{
    private static readonly string TableName = "User" +
                                               Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") switch
                                               {
                                                   "Development" => "_develop",
                                                   "Staging" => $"_pr_{Environment.GetEnvironmentVariable("HEAD")}",
                                                   _ => ""
                                               };

    /// <summary>
    ///     -1 not initialized
    ///     0 owner
    ///     1 user
    ///     2 terminated
    /// </summary>
    public int PermissionCode { get; init; } = -1;

    public byte[] UserId { get; init; } = [];
    public string Name { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public string Bio { get; set; } = "";

    /// <summary>
    ///     -1 not initialized
    ///     0 not verify
    ///     1 verified
    ///     2 banned
    /// </summary>
    public int State { get; set; } = -1;

    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
    public string? Mail { get; set; }

    public Dictionary<byte[], int> GetPermission(OTSClient client)
    {
        var batchGetRowRequest = new BatchGetRowRequest();
        batchGetRowRequest.Add(TableName, [
            new PrimaryKey
            {
                { "PermissionCode", new ColumnValue(0) },
                { "UserID", new ColumnValue(UserId) }
            },
            new PrimaryKey
            {
                { "PermissionCode", new ColumnValue(1) },
                { "UserID", new ColumnValue(UserId) }
            },
            new PrimaryKey
            {
                { "PermissionCode", new ColumnValue(2) },
                { "UserID", new ColumnValue(UserId) }
            }
        ]);
        var resp = client.BatchGetRow(batchGetRowRequest);
        var result = new Dictionary<byte[], int>();
        foreach (var row in resp.RowDataGroupByTable[TableName])
        {
            if (!row.IsOK) continue;
            result[row.PrimaryKey["UserID"].AsBinary()] = row.PrimaryKey["PermissionCode"].AsLong().ToSafeInt()!.Value;
        }

        return result;
    }

    public User GetSingle(OTSClient client)
    {
        if (PermissionCode < 0) throw new ValidationException("Database.User.BadPermission");
        var row = client.GetRow(new GetRowRequest(TableName, new PrimaryKey
        {
            { "PermissionCode", new ColumnValue(PermissionCode) },
            { "UserID", new ColumnValue(UserId) }
        })).Row;
        if (row.GetColumns().Length == 0)
            throw new NotFoundException("Database.User.NotFound");
        foreach (var col in row.GetColumns())
            switch (col.Name)
            {
                case "Name":
                    Name = col.Value.AsString();
                    continue;
                case "AvatarUrl":
                    AvatarUrl = col.Value.AsString();
                    continue;
                case "Bio":
                    Bio = col.Value.AsString();
                    continue;
                case "State":
                    State = col.Value.AsLong().ToSafeInt() ??
                            throw new ValidationException("User.GetSingle.State.OutOfRange");
                    continue;
                case "CreatedAt":
                    CreatedAt = col.Value.AsLong();
                    continue;
                case "UpdatedAt":
                    UpdatedAt = col.Value.AsLong();
                    continue;
                case "Mail":
                    Mail = col.Value.AsString();
                    continue;
            }

        return this;
    }

    public User GetSingle(OTSClient client, int permission)
    {
        if (permission < 0) throw new ValidationException("Database.User.BadPermission");
        var row = client.GetRow(new GetRowRequest(TableName, new PrimaryKey
        {
            { "PermissionCode", new ColumnValue(permission) },
            { "UserID", new ColumnValue(UserId) }
        })).Row;
        if (row.GetColumns().Length == 0)
            throw new NotFoundException("Database.User.NotFound");
        foreach (var col in row.GetColumns())
            switch (col.Name)
            {
                case "Name":
                    Name = col.Value.AsString();
                    continue;
                case "AvatarUrl":
                    AvatarUrl = col.Value.AsString();
                    continue;
                case "Bio":
                    Bio = col.Value.AsString();
                    continue;
                case "State":
                    State = col.Value.AsLong().ToSafeInt() ??
                            throw new ValidationException("User.GetSingle.State.OutOfRange");
                    continue;
                case "CreatedAt":
                    CreatedAt = col.Value.AsLong();
                    continue;
                case "UpdatedAt":
                    UpdatedAt = col.Value.AsLong();
                    continue;
                case "Mail":
                    Mail = col.Value.AsString();
                    continue;
            }

        return this;
    }

    public void Create(OTSClient client)
    {
        try
        {
            client.PutRow(new PutRowRequest(TableName,
                new Condition(RowExistenceExpectation.EXPECT_NOT_EXIST),
                new PrimaryKey
                {
                    { "PermissionCode", new ColumnValue(PermissionCode) },
                    { "UserID", new ColumnValue(UserId) }
                },
                new AttributeColumns
                {
                    { "Name", new ColumnValue(Name) },
                    { "AvatarUrl", new ColumnValue(AvatarUrl) },
                    { "Bio", new ColumnValue(Bio) },
                    { "State", new ColumnValue(State) },
                    { "CreatedAt", new ColumnValue(CreatedAt) },
                    { "UpdatedAt", new ColumnValue(UpdatedAt) }
                }));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public void Delete(OTSClient client)
    {
        try
        {
            client.DeleteRow(new DeleteRowRequest(TableName, new Condition(RowExistenceExpectation.EXPECT_EXIST),
                new PrimaryKey
                {
                    { "PermissionCode", new ColumnValue(PermissionCode) },
                    { "UserID", new ColumnValue(UserId) }
                }));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public void ActiveAccount(OTSClient client)
    {
        if (State != 0) throw new ValidationException("Database.User.BadState");
        if (PermissionCode is > 1 or < 0) throw new ValidationException("Database.User.BadPermission");
        if (UserId.Length == 0) throw new ValidationException("Database.User.IllegalValue.UserID");

        var updateOfAttribute = new UpdateOfAttribute();
        updateOfAttribute.AddAttributeColumnToPut("State", new ColumnValue(1));
        updateOfAttribute.AddAttributeColumnToPut("UpdatedAt",
            new ColumnValue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        client.UpdateRow(new UpdateRowRequest(TableName, new Condition
            {
                RowExistenceExpect = RowExistenceExpectation.EXPECT_EXIST,
                ColumnCondition = new RelationalCondition(
                    "State",
                    CompareOperator.LESS_THAN,
                    new ColumnValue(1))
                {
                    PassIfMissing = false,
                    LatestVersionsOnly = true
                }
            },
            new PrimaryKey
            {
                { "PermissionCode", new ColumnValue(PermissionCode) },
                { "UserID", new ColumnValue(UserId) }
            }, updateOfAttribute));
    }

    public void BanAccount(OTSClient client)
    {
        if (UserId.Length == 0) throw new ValidationException("Database.User.IllegalValue.UserID");
        var updateOfAttribute = new UpdateOfAttribute();
        updateOfAttribute.AddAttributeColumnToPut("State", new ColumnValue(2));
        updateOfAttribute.AddAttributeColumnToPut("UpdatedAt",
            new ColumnValue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        var compositeCondition = new CompositeCondition(LogicOperator.AND);
        compositeCondition.AddCondition(new RelationalCondition(
                "State", CompareOperator.NOT_EQUAL, new ColumnValue(-1))
            { PassIfMissing = false, LatestVersionsOnly = true });
        compositeCondition.AddCondition(new RelationalCondition(
                "State", CompareOperator.NOT_EQUAL, new ColumnValue(2))
            { PassIfMissing = false, LatestVersionsOnly = true });
        client.UpdateRow(new UpdateRowRequest(TableName, new Condition
            {
                RowExistenceExpect = RowExistenceExpectation.EXPECT_EXIST,
                ColumnCondition = compositeCondition
            },
            new PrimaryKey
            {
                { "PermissionCode", new ColumnValue(PermissionCode) },
                { "UserID", new ColumnValue(UserId) }
            }, updateOfAttribute));
    }

    public Task<UpdateRowResponse> UpdateProfile(OTSClient client, Dictionary<string, ColumnValue>? updates,
        string[]? deletes)
    {
        try
        {
            if (State is not (0 or 1)) throw new ValidationException("Database.User.BadState");
            if (PermissionCode is < 0 or > 1) throw new ValidationException("Database.User.BadPermission");
            if (UserId.Length == 0) throw new ValidationException("Database.User.IllegalValue.UserID");
            var updateOfAttribute = new UpdateOfAttribute();
            if (updates == null && deletes == null) throw new ValidationException("Database.User.EmptyRequest");

            if (updates != null)
            {
                if (updates.ContainsKey("UserID") || updates.ContainsKey("CreateAt") || updates.ContainsKey("UpdateAt"))
                    throw new ConflictException("Database.User.Update.Denied");
                foreach (var col in updates) updateOfAttribute.AddAttributeColumnToPut(col.Key, col.Value);
            }

            // ReSharper disable once InvertIf
            if (deletes != null)
            {
                if (deletes.Contains("UserID") || deletes.Contains("CreateAt") || deletes.Contains("UpdateAt") ||
                    deletes.Contains("Mail"))
                    throw new ConflictException("Database.User.Delete.Denied");
                foreach (var target in deletes) updateOfAttribute.AddAttributeColumnToDelete(target);
            }

            var stateCond = new CompositeCondition(LogicOperator.AND);
            stateCond.AddCondition(new RelationalCondition(
                    "State", CompareOperator.NOT_EQUAL, new ColumnValue(-1))
                { PassIfMissing = false, LatestVersionsOnly = true });
            stateCond.AddCondition(new RelationalCondition(
                    "State", CompareOperator.NOT_EQUAL, new ColumnValue(2))
                { PassIfMissing = false, LatestVersionsOnly = true });
            return client.UpdateRowAsync(new UpdateRowRequest(TableName, new Condition
            {
                RowExistenceExpect = RowExistenceExpectation.EXPECT_EXIST,
                ColumnCondition = stateCond
            }, new PrimaryKey
            {
                { "PermissionCode", new ColumnValue(PermissionCode) },
                { "UserID", new ColumnValue(UserId) }
            }, updateOfAttribute));
        }
        catch (Exception exception)
        {
            return Task.FromException<UpdateRowResponse>(exception);
        }
    }
}
/* User */

/* EmailVerification */
public class EmailVerification
{
    private static readonly string TableName = "EmailVerification" +
                                               Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") switch
                                               {
                                                   "Development" => "_develop",
                                                   "Staging" => $"_pr_{Environment.GetEnvironmentVariable("HEAD")}",
                                                   _ => ""
                                               };

    public string Mail { get; set; } = "";
    public int Purpose { get; set; }

    public byte[]? TokenHash { get; set; }
    public byte[]? UserId { get; set; }

    public long? CreatedAt { get; set; }
    // ExpiresAt 直接使用ots的表级TTL替代

    public void GetSingle(OTSClient client)
    {
        var resp = client.GetRow(new GetRowRequest(TableName, new PrimaryKey
        {
            { "Email", new ColumnValue(Mail) },
            { "Purpose", new ColumnValue(Purpose) }
        }));
        if (resp.Row.GetColumns().Length == 0)
            throw new NotFoundException("Database.EmailVerification.NotFound");
        foreach (var col in resp.Row.GetColumns())
            switch (col.Name)
            {
                case "TokenHash": TokenHash = col.Value.AsBinary(); break;
                case "UserId": UserId = col.Value.AsBinary(); break;
                case "CreatedAt": CreatedAt = col.Value.AsLong(); break;
            }
    }

    public void Create(OTSClient client)
    {
        try
        {
            client.PutRow(new PutRowRequest(TableName,
                new Condition(RowExistenceExpectation.IGNORE),
                new PrimaryKey
                {
                    { "Email", new ColumnValue(Mail) },
                    { "Purpose", new ColumnValue(Purpose) }
                },
                new AttributeColumns
                {
                    { "TokenHash", new ColumnValue(TokenHash!) },
                    { "UserId", new ColumnValue(UserId!) },
                    { "CreatedAt", new ColumnValue(CreatedAt ?? 0) }
                }));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public void Delete(OTSClient client)
    {
        client.DeleteRow(new DeleteRowRequest(TableName,
            new Condition(RowExistenceExpectation.EXPECT_EXIST),
            new PrimaryKey
            {
                { "Email", new ColumnValue(Mail) },
                { "Purpose", new ColumnValue(Purpose) }
            }));
    }
}
/* EmailVerification */

/* UserIdentity */
public class UserIdentity
{
    private static readonly string TableName = "UserIdentity" +
                                               Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") switch
                                               {
                                                   "Development" => "_develop",
                                                   "Staging" => $"_pr_{Environment.GetEnvironmentVariable("HEAD")}",
                                                   _ => ""
                                               };

    public string IdentityType { get; set; } = "";
    public string IdentityKey { get; set; } = "";
    public byte[] UserId { get; set; } = [];

    /// <summary>
    ///     0 normal
    ///     1 suspicious
    ///     2 locked
    ///     3 security banned
    /// </summary>
    public int State { get; set; }

    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }

    // 动态认证列: srp / github / google / passkey / ...
    public byte[] AuthData { get; set; } = [];

    /// <summary>
    ///     ALERT: 不会执行任何检查，请自行在编解码前准备好
    /// </summary>
    /// <param name="payload" />
    public void AuthMarshal<T>(T payload) where T : class, IAuthData
    {
        AuthData = Serialize(payload);
    }

    public T AuthUnmarshal<T>() where T : class, IAuthData
    {
        return AuthData.Any()
            ? Deserialize<T>(AuthData)
            : throw new ValidationException("UserIdentity.AuthUnmarshal.Null");
    }

    public void Create(OTSClient client)
    {
        client.PutRow(new PutRowRequest(TableName,
            new Condition(RowExistenceExpectation.EXPECT_NOT_EXIST),
            new PrimaryKey
            {
                { "IdentityType", new ColumnValue(IdentityType) },
                { "IdentityKey", new ColumnValue(IdentityKey) }
            },
            new AttributeColumns
            {
                {
                    "UserID",
                    UserId.Length > 0
                        ? new ColumnValue(UserId)
                        : throw new ValidationException("UserIdentity.Create.UserId.Empty")
                },
                { "AuthData", new ColumnValue(AuthData) }
            }));
    }


    public T ReadAuthData<T>(OTSClient client) where T : class, IAuthData
    {
        var cond = new CompositeCondition(LogicOperator.OR);
        cond.AddCondition(new RelationalCondition("State", CompareOperator.LESS_THAN, new ColumnValue(3)));
        cond.AddCondition(new RelationalCondition("UserID", CompareOperator.NOT_EQUAL, new ColumnValue([])));
        cond.AddCondition(new RelationalCondition("AuthData", CompareOperator.NOT_EQUAL, new ColumnValue([])));
        var resp = client.GetRow(new GetRowRequest(TableName, new PrimaryKey
        {
            { "IdentityType", new ColumnValue(IdentityType) },
            { "IdentityKey", new ColumnValue(IdentityKey) }
        }, ["AuthData", "UserID"], cond));
        if (resp.Row.GetColumns().Length == 0)
            throw new NotFoundException("UserIdentity.Read.AuthData.Failed");
        foreach (var col in resp.Row.GetColumns())
            switch (col.Name)
            {
                case "AuthData":
                    AuthData = col.Value.AsBinary();
                    break;
                case "UserID":
                    UserId = col.Value.AsBinary();
                    break;
            }

        return AuthUnmarshal<T>();
    }
}
/* UserIdentity */

/* Session */
public class Session
{
    private static readonly string TableName = "Session" +
                                               Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") switch
                                               {
                                                   "Development" => "_develop",
                                                   "Staging" => $"_pr_{Environment.GetEnvironmentVariable("HEAD")}",
                                                   _ => ""
                                               };

    public byte[] UserId { get; set; } = [];
    public byte[] SessionId { get; set; } = [];
    public byte[] DeviceInfo { get; set; } = [];
    public byte[] IpHash { get; set; } = [];
    public string UserAgent { get; set; } = "";
    public long CreatedAt { get; set; }
    public long LastSeenAt { get; set; }
    public long ExpiresAt { get; set; }

    /// <summary>
    ///     false = SRP challenge sent (pending verification)
    ///     true = authenticated
    /// </summary>
    public bool SrpState { get; set; }

    public byte[] SrpB { get; set; } = [];
    public byte[] SrpA { get; set; } = [];
    public byte[] SrpServerSecret { get; set; } = [];
    public byte[] Seed { get; set; } = [];

    public void Create(OTSClient client)
    {
        try
        {
            client.PutRow(new PutRowRequest(TableName,
                new Condition(RowExistenceExpectation.EXPECT_NOT_EXIST),
                new PrimaryKey
                {
                    { "UserID", new ColumnValue(UserId) },
                    { "SessionID", new ColumnValue(SessionId) }
                },
                new AttributeColumns
                {
                    { "DeviceInfo", new ColumnValue(DeviceInfo) },
                    { "IPHash", new ColumnValue(IpHash) },
                    { "UserAgent", new ColumnValue(UserAgent) },
                    { "CreatedAt", new ColumnValue(CreatedAt) },
                    { "LastSeenAt", new ColumnValue(LastSeenAt) },
                    { "ExpiresAt", new ColumnValue(ExpiresAt) },
                    { "SrpState", new ColumnValue(SrpState) },
                    { "SrpB", new ColumnValue(SrpB) },
                    { "SrpA", new ColumnValue(SrpA) },
                    { "SrpServerSecret", new ColumnValue(SrpServerSecret) },
                    { "Seed", new ColumnValue(Seed) }
                }));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public Session GetSingle(OTSClient client)
    {
        var resp = client.GetRow(new GetRowRequest(TableName, new PrimaryKey
        {
            { "UserID", new ColumnValue(UserId) },
            { "SessionID", new ColumnValue(SessionId) }
        }));
        if (resp.Row.GetColumns().Length == 0)
            throw new NotFoundException("Database.Session.NotFound");
        foreach (var col in resp.Row.GetColumns())
            switch (col.Name)
            {
                case "DeviceInfo":
                    DeviceInfo = col.Value.AsBinary();
                    continue;
                case "IPHash":
                    IpHash = col.Value.AsBinary();
                    continue;
                case "UserAgent":
                    UserAgent = col.Value.AsString();
                    continue;
                case "CreatedAt":
                    CreatedAt = col.Value.AsLong();
                    continue;
                case "LastSeenAt":
                    LastSeenAt = col.Value.AsLong();
                    continue;
                case "ExpiresAt":
                    ExpiresAt = col.Value.AsLong();
                    continue;
                case "SrpState":
                    SrpState = col.Value.AsBoolean();
                    continue;
                case "SrpB":
                    SrpB = col.Value.AsBinary();
                    continue;
                case "SrpA":
                    SrpA = col.Value.AsBinary();
                    continue;
                case "SrpServerSecret":
                    SrpServerSecret = col.Value.AsBinary();
                    continue;
                case "Seed":
                    Seed = col.Value.AsBinary();
                    continue;
            }

        return this;
    }

    public void UpdateToAuthenticated(OTSClient client, byte[] seed)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var updateOfAttribute = new UpdateOfAttribute();
        updateOfAttribute.AddAttributeColumnToPut("SrpState", new ColumnValue(true));
        updateOfAttribute.AddAttributeColumnToPut("Seed", new ColumnValue(seed));
        updateOfAttribute.AddAttributeColumnToPut("LastSeenAt", new ColumnValue(now));
        updateOfAttribute.AddAttributeColumnToPut("ExpiresAt", new ColumnValue(now + 30L * 24 * 60 * 60 * 1000));
        client.UpdateRow(new UpdateRowRequest(TableName, new Condition
            {
                RowExistenceExpect = RowExistenceExpectation.EXPECT_EXIST,
                ColumnCondition = new RelationalCondition(
                    "SrpState",
                    CompareOperator.EQUAL,
                    new ColumnValue(false))
                {
                    PassIfMissing = false,
                    LatestVersionsOnly = true
                }
            },
            new PrimaryKey
            {
                { "UserID", new ColumnValue(UserId) },
                { "SessionID", new ColumnValue(SessionId) }
            }, updateOfAttribute));
    }

    public async void CleanUpSrpData(OTSClient client)
    {
        try
        {
            var updateOfAttribute = new UpdateOfAttribute();
            updateOfAttribute.AddAttributeColumnToDelete("SrpA");
            updateOfAttribute.AddAttributeColumnToDelete("SrpB");
            updateOfAttribute.AddAttributeColumnToDelete("SrpServerSecret");
            updateOfAttribute.AddAttributeColumnToPut("LastSeenAt",
                new ColumnValue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
            await client.UpdateRowAsync(new UpdateRowRequest(TableName,
                new Condition(RowExistenceExpectation.EXPECT_EXIST), new PrimaryKey
                {
                    { "UserID", new ColumnValue(UserId) },
                    { "SessionID", new ColumnValue(SessionId) }
                }, updateOfAttribute));
        }
        catch (Exception e)
        {
#if DEBUG
            Console.WriteLine(e.StackTrace);
#endif
            Console.WriteLine(e.Message);
        }
    }

    public void UpdateSeed(OTSClient client, byte[] oldSeed, byte[] newSeed)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var updateOfAttribute = new UpdateOfAttribute();
        updateOfAttribute.AddAttributeColumnToPut("Seed", new ColumnValue(newSeed));
        updateOfAttribute.AddAttributeColumnToPut("LastSeenAt", new ColumnValue(now));
        var cond = new CompositeCondition(LogicOperator.AND);
        cond.AddCondition(new RelationalCondition(
                "SrpState", CompareOperator.EQUAL, new ColumnValue(true))
            { PassIfMissing = false, LatestVersionsOnly = true });
        cond.AddCondition(new RelationalCondition(
                "Seed", CompareOperator.EQUAL, new ColumnValue(oldSeed))
            { PassIfMissing = false, LatestVersionsOnly = true });
        client.UpdateRow(new UpdateRowRequest(TableName, new Condition
            {
                RowExistenceExpect = RowExistenceExpectation.EXPECT_EXIST,
                ColumnCondition = cond
            },
            new PrimaryKey
            {
                { "UserID", new ColumnValue(UserId) },
                { "SessionID", new ColumnValue(SessionId) }
            }, updateOfAttribute));
    }

    public void Delete(OTSClient client)
    {
        try
        {
            client.DeleteRow(new DeleteRowRequest(TableName, new Condition(RowExistenceExpectation.IGNORE),
                new PrimaryKey
                {
                    { "UserID", new ColumnValue(UserId) },
                    { "SessionID", new ColumnValue(SessionId) }
                }));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
/* Session */

/* Config */
public class Config
{
    private static readonly string TableName = "Config" +
                                               Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") switch
                                               {
                                                   "Development" => "_develop",
                                                   "Staging" => $"_pr_{Environment.GetEnvironmentVariable("HEAD")}",
                                                   _ => ""
                                               };

    public string Pk { get; set; } = "CONFIG";
    public string Sk { get; set; } = "CURRENT";
    public int SchemaVersion { get; set; }
    public int ConfigVersion { get; set; }
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Theme { get; set; } = "";
    public string Language { get; set; } = "";
    public string Footer { get; set; } = "";
    public string Copyright { get; set; } = "";
    public string Notice { get; set; } = "";
    public bool AllowComment { get; set; }
    public bool AllowRegister { get; set; }
    public string HomepageLayout { get; set; } = "";
}
/* Config */