using Aliyun.OTS;
using Aliyun.OTS.DataModel;
using Aliyun.OTS.Request;
using MessagePack;
using Tea.Utils;

namespace backend.database;
using static MessagePackSerializer;
/* Post */
public class Post
{
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
    public string CommentId { get; set; } = "";
    public byte[] UserId { get; set; } = [];
}
/* CommentLike */

/* User */
public class User
{
    public int PermissionCode { get; init; }
    /// <summary>
    /// 0 owner
    /// 1 user
    /// 2 terminated
    /// </summary>
    public byte[] UserId { get; init; } = [];
    public string Name { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public string Bio { get; set; } = "";
    public int State { get; set; }
    ///  <summary>
    ///  0 not verify
    ///  1 verified
    ///  2 banned
    ///  </summary>
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
    public User GetSingle(OTSClient client)
    {
        foreach (var col in client.GetRow(new GetRowRequest("User", new PrimaryKey
                 {
                     { "PermissionCode", new ColumnValue(PermissionCode) },
                     { "UserID", new ColumnValue(UserId) }
                 })).Row.GetColumns())
            switch (col.Name)
            {
                case "Name": Name = col.Value.AsString(); break;
                case "AvatarUrl": AvatarUrl = col.Value.AsString(); break;
                case "Bio": Bio = col.Value.AsString(); break;
                case "State":
                    State = col.Value.AsLong().ToSafeInt() ?? throw new InvalidDataException("User.GetSingle.State.OutOfRange"); break;
                case "CreatedAt": CreatedAt = col.Value.AsLong(); break;
                case "UpdatedAt": UpdatedAt = col.Value.AsLong(); break;
            }
        return this;
    }

    public bool IsExist(OTSClient client) =>
        client.GetRow(new GetRowRequest("User", new PrimaryKey
        {
            { "PermissionCode", new ColumnValue(PermissionCode) },
            { "UserID", new ColumnValue(UserId) }
        })).Row.GetColumns().Length != 0;

    public void Create(OTSClient client)
    {
        try
        {
            client.PutRow(new PutRowRequest("User",
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
            client.DeleteRow(new DeleteRowRequest("User",new Condition(RowExistenceExpectation.EXPECT_EXIST),new PrimaryKey
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
}
/* User */

/* UserIdentity */
public class UserIdentity
{
    public string IdentityType { get; init; } = "";
    public string IdentityKey { get; set; } = "";
    public byte[] UserId { get; init; } = [];
    public int State { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }

    // 动态认证列: srp / github / google / passkey / ...
    public byte[] AuthData { get; set; } = [];
    /// <summary>
    /// ALEART: 不会执行任何检查，请自行在编解码前准备好
    /// </summary>
    /// <param name="playload"/>
    public void AuthMarshal(SrpAuthData playload) => AuthData = Serialize(playload);
    public void AuthMarshal(OAuthAuthData playload) => AuthData = Serialize(playload);
    public void AuthMarshal(PasskeyAuthData playload) => AuthData = Serialize(playload);
    
    
    
    public bool IsExist(OTSClient client) =>
        client.GetRange(new GetRangeRequest(
            "UserIdentity",
            GetRangeDirection.Forward,
            new PrimaryKey
            {
                { "IdentityType", IdentityType != "" ? new ColumnValue(IdentityType) : ColumnValue.INF_MIN },
                { "IdentityKey", ColumnValue.INF_MIN }
            },
            new PrimaryKey
            {
                { "IdentityType", IdentityType != "" ? new ColumnValue(IdentityType) : ColumnValue.INF_MAX },
                { "IdentityKey", ColumnValue.INF_MAX }
            },
            limit: 100
        )).RowDataList.Any(row => {
            if (UserId.Length == 0) return true;
            return row.AttributeColumns.TryGetValue("UserID", out var col)
                && col.AsBinary().AsSpan().SequenceEqual(UserId);
        });
    public void Create(OTSClient client)
    {
        try
        {
            client.PutRow(new PutRowRequest("UserIdentity",
                new Condition(RowExistenceExpectation.EXPECT_NOT_EXIST),
                new PrimaryKey
                {
                    { "IdentityType", new ColumnValue(IdentityType) },
                    { "IdentityKey", new ColumnValue(IdentityKey) },
                },
                new AttributeColumns
                {
                    {
                        "UserID",
                        UserId.Length > 0
                            ? new ColumnValue(UserId)
                            : throw new InvalidDataException("UserIdentity.Create.UserId.Empty")
                    },
                    {"AuthData", new ColumnValue(AuthData) }
                }));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
/* UserIdentity */

/* Session */
public class Session
{
    public byte[] UserId { get; set; } = [];
    public string SessionId { get; set; } = "";
    public byte[] RefreshTokenHash { get; set; } = [];
    public byte[] DeviceInfo { get; set; } = [];
    public byte[] IpHash { get; set; } = [];
    public string UserAgent { get; set; } = "";
    public long CreatedAt { get; set; }
    public long LastSeenAt { get; set; }
    public long ExpiresAt { get; set; }
}
/* Session */

/* Config */
public class Config
{
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