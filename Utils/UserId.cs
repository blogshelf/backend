using System.Buffers.Binary;
using System.Security.Cryptography;

namespace backend.Utils;

public static class UserId
{
    private const int Size = 1024;
    private const int TimestampSize = 8;
    private const int IpSize = 16;
    private const int EphemeralPubKeySize = 65;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int Overhead = EphemeralPubKeySize + NonceSize + TagSize;
    private const int PlaintextSize = Size - Overhead;

    public static byte[] Generate(DateTimeOffset timestamp, byte[] clientIp,
        ECDiffieHellmanPublicKey recipientPublicKey)
    {
        var plaintext = new byte[PlaintextSize];

        // CreatedTimestamp: 8 bytes, big-endian
        BinaryPrimitives.WriteInt64BigEndian(plaintext.AsSpan(0, TimestampSize),
            timestamp.ToUnixTimeMilliseconds());

        // ClientIP: 固定 16 字节槽位
        // IPv4: 前 4 字节存储地址，后 12 字节已为零
        // IPv6: 填满 16 字节
        if (clientIp.Length is 4 or 16)
            Buffer.BlockCopy(clientIp, 0, plaintext, TimestampSize, Math.Min(clientIp.Length, IpSize));

        // Random: 填充剩余空间
        RandomNumberGenerator.Fill(plaintext.AsSpan(TimestampSize + IpSize));

        // ECIES 加密
        using var ephemeral = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var sharedSecret = ephemeral.DeriveKeyMaterial(recipientPublicKey);

        // 使用前 32 字节作为 AES-256 密钥
        var aesKey = sharedSecret.AsSpan(0, 32).ToArray();

        using var aesGcm = new AesGcm(aesKey, TagSize);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[PlaintextSize];
        var tag = new byte[TagSize];
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

        // 打包: [ephemeralPubKey (65)][nonce (12)][ciphertext (931)][tag (16)] = 1024
        var result = new byte[Size];
        var ephemeralPubKey = ephemeral.ExportSubjectPublicKeyInfo();
        Buffer.BlockCopy(ephemeralPubKey, 0, result, 0, EphemeralPubKeySize);
        Buffer.BlockCopy(nonce, 0, result, EphemeralPubKeySize, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, result, EphemeralPubKeySize + NonceSize, PlaintextSize);
        Buffer.BlockCopy(tag, 0, result, EphemeralPubKeySize + NonceSize + PlaintextSize, TagSize);

        return result;
    }
}