using MessagePack;

namespace backend.models;

[MessagePackObject]
public sealed record RegisterSrp
{
    [Key("username")] public required string Username { get; init; }
    [Key("verifier")] public required byte[] Verifier { get; init; }
    [Key("salt")] public required byte[] Salt { get; init; }
}