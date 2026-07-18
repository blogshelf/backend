using System.Security.Cryptography;
using Aliyun.OTS;
using backend.exceptions;
using backend.initialization;
using backend.models;
using backend.services;
using backend.Utils;
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
            State = 0
        };
        userData.Create(client);

        var userIdentity = new UserIdentity
        {
            UserId = userId,
            IdentityType = "srp",
            IdentityKey = regData.Username
        };
        userIdentity.AuthMarshal(new SrpAuthData
        {
            Verifier = regData.Verifier,
            Salt = regData.Salt
        });
        userIdentity.Create(client);
    }

    public LoginBeginReturn BeginSrpLogin(LoginSrpStart loginCtx, User table, OtpService otp)
    {
        OtpVerifyResult? verifyRes = null;
        if (loginCtx is { OtpToken: not null, Mail: not null })
            verifyRes = otp.Verify(loginCtx.Mail, loginCtx.OtpToken);
        switch (verifyRes)
        {
            case OtpVerifyResult.Valid:
            case null: table.ActiveAccount(client); break;
            case OtpVerifyResult.Expired: throw new ValidationException("Verify.Mail.Expired");
            case OtpVerifyResult.InvalidCode: throw new ValidationException("Verify.Mail.ErrorCode");
            case OtpVerifyResult.NotFound: throw new ValidationException("Verify.Mail.NotFound");
            default: throw new ArgumentOutOfRangeException();
        }

        var userIdentity = new UserIdentity
        {
            IdentityType = "srp",
            IdentityKey = loginCtx.Username
        };
        var authData = userIdentity.ReadAuthData<SrpAuthData>(client);
        return VerifyLoginBegin(loginCtx.Username, loginCtx.A, authData.Verifier, authData.Salt);
    }

    public LoginCompleteReturn CompleteSrpReturn(LoginSrpComplete completeCtx, User user, UserIdentity id,
        Session session)
    {
        if (user.GetPermission(client)[session.UserId].Equals(2))
            throw new ConflictException("User.Account.Terminated");

        id.IdentityType = "srp";
        var authData = id.ReadAuthData<SrpAuthData>(client);

        // ReSharper disable once InconsistentNaming
        var K = ComputeK(session.SrpA, authData.Verifier, session.SrpServerSecret, session.SrpB);
        if (!VerifyClientProof(user.Name, authData.Salt, session.SrpA, session.SrpB, K, completeCtx.M))
        {
            session.Delete(client);
            return new LoginCompleteReturn
            {
                Proofed = false,
                ServerProof = null,
                EncryptedSeed = null
            };
        }

        var seed = RandomNumberGenerator.GetBytes(32);
        var encryptedSeed = AesGcmHelper.Encrypt(K, seed);

        session.UpdateToAuthenticated(client, seed);
        session.CleanUpSrpData(client);

        return new LoginCompleteReturn
        {
            Proofed = true,
            ServerProof = ComputeServerProof(session.SrpA,
                ComputeClientM1(user.Name, authData.Salt, session.SrpA, session.SrpB, K), K),
            EncryptedSeed = encryptedSeed
        };
    }
}