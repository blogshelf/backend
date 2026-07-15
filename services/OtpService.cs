using System.Security.Cryptography;
using Aliyun.OTS;
using backend.database;

namespace backend.services;

public class OtpService(OTSClient client)
{
    private const int RateLimitMs = 30 * 1000;
    private const int OtpByteLength = 32;

    public byte[] Generate()
    {
        var bytes = new byte[OtpByteLength];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    private byte[] HashOtp(byte[] otp) =>
        SHA256.HashData(otp);

    public void Store(string email, byte[] userId, byte[] otp)
    {
        new EmailVerification
        {
            Mail = email,
            Purpose = 0,
            TokenHash = HashOtp(otp),
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        }.Create(client);
    }

    public OtpVerifyResult Verify(string email, byte[] otp)
    {
        var ev = new EmailVerification { Mail = email, Purpose = 0 };
        try
        {
            ev.GetSingle(client);
        }
        catch (IOException)
        {
            return OtpVerifyResult.NotFound;
        }
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (ev.TokenHash is null)
            return OtpVerifyResult.NotFound;

        if (otp.Length != OtpByteLength || !HashOtp(otp).AsSpan().SequenceEqual(ev.TokenHash))
            return OtpVerifyResult.InvalidCode;

        return OtpVerifyResult.Valid;
    }

    public byte[]? GetUserId(string email)
    {
        var ev = new EmailVerification { Mail = email, Purpose = 0 };
        try
        {
            ev.GetSingle(client);
        }
        catch (IOException)
        {
            return null;
        }
        return ev.UserId;
    }

    public bool IsRateLimited(string email)
    {
        var ev = new EmailVerification { Mail = email, Purpose = 0 };
        try
        {
            ev.GetSingle(client);
        }
        catch (IOException)
        {
            return false;
        }
        return ev.CreatedAt is not null && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - ev.CreatedAt.Value < RateLimitMs;
    }

    public void Delete(string email)
    {
        new EmailVerification { Mail = email, Purpose = 0 }.Delete(client);
    }
}

public enum OtpVerifyResult
{
    Valid,
    NotFound,
    InvalidCode,
    Expired,
    RateLimited
}
