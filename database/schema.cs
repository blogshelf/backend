namespace backend.database;

/* Post */
public class Post
{
    public string PostID { get; set; } = "";
    public int MonthID { get; set; }
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
    public string PostID { get; set; } = "";
    public string CommentID { get; set; } = "";
    public string? ParentCommentID { get; set; }
    public byte[] UserID { get; set; } = [];
    public string Body { get; set; } = "";
    public int State { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
    public byte[] UserSignature { get; set; } = [];
    public string ClientKeyID { get; set; } = "";
}
/* Comment */

/* CommentLike */
public class CommentLike
{
    public string CommentID { get; set; } = "";
    public byte[] UserID { get; set; } = [];
}
/* CommentLike */

/* User */
public class User
{
    public int PermissionCode { get; set; }
    public byte[] UserID { get; set; } = [];
    public string Name { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public string Bio { get; set; } = "";
    public int State { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
}
/* User */

/* UserIdentity */
public class UserIdentity
{
    public string IdentityType { get; set; } = "";
    public string IdentityKey { get; set; } = "";
    public byte[] UserID { get; set; } = [];
    public int State { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }

    // 动态认证列: srp / github / google / passkey / ...
    public Dictionary<string, byte[]> AuthData { get; set; } = new();
}
/* UserIdentity */

/* Session */
public class Session
{
    public byte[] UserID { get; set; } = [];
    public string SessionID { get; set; } = "";
    public byte[] RefreshTokenHash { get; set; } = [];
    public byte[] DeviceInfo { get; set; } = [];
    public byte[] IPHash { get; set; } = [];
    public string UserAgent { get; set; } = "";
    public long CreatedAt { get; set; }
    public long LastSeenAt { get; set; }
    public long ExpiresAt { get; set; }
}
/* Session */

/* Config */
public class Config
{
    public string PK { get; set; } = "CONFIG";
    public string SK { get; set; } = "CURRENT";
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
