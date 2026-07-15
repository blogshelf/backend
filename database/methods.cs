using Aliyun.OTS;
using backend.initialization;
using backend.models;
using Jdenticon;

namespace backend.database;

public class Methods(OTSClient client)
{
    private ILogger Log { get; } = Logger.LogFactory.CreateLogger("Register.Srp");

    public void Register(RegisterSrp regData, bool isOwner,
        byte[] userId)
    {
        var userData = new User
        {
            PermissionCode = isOwner ? 0 : 1,
            UserId = userId
        };
        if (userData.IsExist(client))
            throw new InvalidOperationException("Database.User.RepeatingData");

        if (userData.GetSingle(client).Name == regData.Username)
            throw new InvalidOperationException("Database.User.RepeatingData");

        userData.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        userData.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        userData.Name = regData.Username;
        userData.Bio = "Bio.Missing.Default";
        userData.AvatarUrl =
            /* 默认自动生成头像，需要前端自行放大 */
            $"data:image/svg+xml,{Identicon.FromHash(userId, 16).ToSvg(false)}";
        userData.State = 0;
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
        };
        if (userIdentity.IsExist(client))
        {
            /* 一般情况下这不可能 */
            throw new SystemException("Database.Holyshit.IsImpossible");
        }
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
}