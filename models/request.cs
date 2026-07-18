using MessagePack;

namespace backend.models;

[MessagePackObject]
public sealed record RegisterSrp
{
    [Key("username")] public required string Username { get; init; }
    [Key("verifier")] public required byte[] Verifier { get; init; }
    [Key("salt")] public required byte[] Salt { get; init; }
}

[MessagePackObject]
public sealed record VerifyForMail
{
    [Key("user_id")] public required byte[] UserId { get; init; }
    [Key("email")] public required string Mail { get; init; }
}

[MessagePackObject]
public sealed record LoginSrpStart
{
    [Key("username")] public required string Username { get; init; }
    [Key("A")] public required byte[] A { get; init; }
    [Key("otp_token")] public byte[]? OtpToken { get; init; }
    [Key("mail")] public string? Mail { get; init; }
}

[MessagePackObject]
public sealed record LoginSrpComplete
{
    [Key("token")] public required byte[] Token { get; init; }
    [Key("M")] public required byte[] M { get; init; }
    [Key("user_id")] public required byte[] UserId { get; init; }
}