using System.Security.Cryptography;
using MessagePack;

namespace backend.Utils;

public static class RequestTokenHelper
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static byte[] DeriveRequestKey(byte[] seed)
    {
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, seed, 32, info: "request-key"u8.ToArray());
    }

    public static byte[] DeriveRenewalKey(byte[] seed)
    {
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, seed, 32, info: "renewal-key"u8.ToArray());
    }

    public static byte[] CreateToken(byte[] requestKey, byte[] plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(requestKey, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[NonceSize + TagSize + plaintext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, plaintext.Length);
        return result;
    }

    public static bool VerifyToken(byte[] requestKey, byte[] token, long serverTimestampMinutes)
    {
        if (token.Length < NonceSize + TagSize)
            return false;

        try
        {
            var nonce = token[..NonceSize];
            var tag = token[NonceSize..(NonceSize + TagSize)];
            var ciphertext = token[(NonceSize + TagSize)..];
            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(requestKey, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            var payload = MessagePackSerializer.Deserialize<AccessTokenPayload>(plaintext);
            return IsTimestampFresh(payload.TimestampMinutes, serverTimestampMinutes);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsTimestampFresh(long tokenTimestamp, long serverTimestamp)
    {
        return Math.Abs(tokenTimestamp - serverTimestamp) <= 1;
    }

    public static byte[] CreateBearerPayload(byte[] requestKey, byte[] userId, byte[] sessionId)
    {
        var timestamp = (long)(DateTimeOffset.UtcNow - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
            .TotalMinutes;
        var salt = RandomNumberGenerator.GetBytes(16);
        var tokenPayload = MessagePackSerializer.Serialize(new AccessTokenPayload
            { TimestampMinutes = timestamp, Salt = salt });
        var encrypted = CreateToken(requestKey, tokenPayload);
        var envelope = new BearerEnvelope { UserId = userId, SessionId = sessionId, Proof = encrypted };
        return MessagePackSerializer.Serialize(envelope);
    }
}

[MessagePackObject(AllowPrivate = true)]
internal record AccessTokenPayload
{
    [Key("t")] public required long TimestampMinutes { get; init; }
    [Key("s")] public required byte[] Salt { get; init; }
}

[MessagePackObject(AllowPrivate = true)]
internal record BearerEnvelope
{
    [Key("u")] public required byte[] UserId { get; init; }
    [Key("s")] public required byte[] SessionId { get; init; }
    [Key("p")] public required byte[] Proof { get; init; }
}