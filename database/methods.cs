using Aliyun.OTS;
using backend.initialization;
using backend.models;
using backend.services;
using Jdenticon;
using static backend.Utils.Tools;

namespace backend.database;

public class Methods(OTSClient client)
{
    private ILogger Log { get; } = Logger.LogFactory.CreateLogger("Database");

    public void Register(RegisterSrp regData, bool isOwner,
        byte[] userId)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var userData = new User
        {
            PermissionCode = isOwner ? 0 : 1,
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
            Name = regData.Username,
            Bio = "Bio.Missing.Default",
            AvatarUrl =
                /* 默认自动生成头像，需要前端自行放大 */
                $"data:image/svg+xml,{Identicon.FromHash(userId, 16).ToSvg(false)}",
            State = 0,
        };
        try
        {
            userData.Create(client);
        }
        catch (Exception e)
        {
#if DEBUG
            Log.LogDebug(e.StackTrace);
#endif
            Log.LogError(e.Message);
            return;
        }

        var userIdentity = new UserIdentity
        {
            UserId = userId,
            IdentityType = "srp",
            IdentityKey = regData.Username,
        };
        userIdentity.AuthMarshal(new SrpAuthData
        {
            Verifier = regData.Verifier,
            Salt = regData.Salt,
        });
        try
        {
            userIdentity.Create(client);
        }
        catch (Exception e)
        {
#if DEBUG
            Log.LogError(e.StackTrace);
#endif
            Log.LogError(e.Message);
        }
    }

    public record SessionMeta
    {
        public byte[] IpHash = [];
        public byte[]? DeviceInfo;
        public string? UserAgent;
    };
    public LoginReturn ServerProof(LoginSrp loginCtx, User table, OtpService otp)
    {
        OtpVerifyResult? verifyRes = null;
        if (loginCtx is { OtpToken: not null, Mail: not null })
            verifyRes = otp.Verify(loginCtx.Mail, loginCtx.OtpToken);
        switch (verifyRes)
        {
            case OtpVerifyResult.Valid: case null: table.ActiveAccount(client);break;
            case OtpVerifyResult.Expired: throw new InvalidOperationException("Verify.Mail.Expired");
            case OtpVerifyResult.InvalidCode: throw new InvalidOperationException("Verify.Mail.ErrorCode");
            case OtpVerifyResult.NotFound: throw new InvalidOperationException("Verify.Mail.NotFound");
            default: throw new ArgumentOutOfRangeException();
        }

        var userIdentity = new UserIdentity
        {
            IdentityType = "srp",
            IdentityKey = loginCtx.Username
        };
        var authData = userIdentity.ReadAuthData<SrpAuthData>(client);
        return VerifyLogin(loginCtx.Username, loginCtx.A, authData.Verifier, authData.Salt);
    }

}