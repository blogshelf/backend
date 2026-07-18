using System.Security.Cryptography;

namespace backend.Utils;

public static class AesGcmHelper
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    private static byte[] DeriveKey(byte[] key)
    {
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, key, KeySize, info: "blogshelf-aes-gcm-v1"u8.ToArray());
    }

    public static byte[] Encrypt(byte[] key, byte[] plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(DeriveKey(key), TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);
        return result;
    }

    public static byte[] Decrypt(byte[] key, byte[] token)
    {
        using var aes = new AesGcm(DeriveKey(key), TagSize);
        var plaintext = new byte[token[(NonceSize + TagSize)..].Length];
        aes.Decrypt(token[..NonceSize], token[(NonceSize + TagSize)..],
            token[NonceSize..(NonceSize + TagSize)], plaintext);
        return plaintext;
    }
}