using MessagePack;

namespace backend.database;

/* Post.Tags: MsgPack<string[]> */
[MessagePackObject]
public class PostTags
{
    [Key("tags")] public string[] Tags { get; set; } = [];
}
/* Post.Tags */

/* Post.AccessPolicy: MsgPack */
[MessagePackObject]
public class PostAccessPolicy
{
    [Key("permission")] public int Permission { get; set; }
    [Key("allow_users")] public string[] AllowUsers { get; set; } = [];
    [Key("deny_users")] public string[] DenyUsers { get; set; } = [];
}
/* Post.AccessPolicy */

/* Session.DeviceInfo: MsgPack */
[MessagePackObject]
public class SessionDeviceInfo
{
    [Key("device_name")] public string DeviceName { get; set; } = "";
    [Key("device_type")] public string DeviceType { get; set; } = "";
    [Key("os")] public string Os { get; set; } = "";
    [Key("browser")] public string Browser { get; set; } = "";
}
/* Session.DeviceInfo */

/* UserIdentity.AuthData values: MsgPack */
public interface IAuthData { }

[MessagePackObject]
public class SrpAuthData : IAuthData
{
    [Key("verifier")] public byte[] Verifier { get; set; } = [];
    [Key("salt")] public byte[] Salt { get; set; } = [];
}
/* SRP */

[MessagePackObject]
public class OAuthAuthData : IAuthData
{
    [Key("subject")] public string Subject { get; set; } = "";
    [Key("username")] public string Username { get; set; } = "";
    [Key("avatar_url")] public string AvatarUrl { get; set; } = "";
}
/* GitHub / Google OAuth */

[MessagePackObject]
public class PasskeyAuthData : IAuthData
{
    [Key("public_key")] public byte[] PublicKey { get; set; } = [];
    [Key("sign_count")] public int SignCount { get; set; }
    [Key("credential_id")] public string CredentialId { get; set; } = "";
}
/* Passkey */
/* UserIdentity.AuthData */
