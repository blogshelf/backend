using Aliyun.OTS;
using Microsoft.Extensions.Configuration;

namespace backend.initialization;

public class Database
{
    public static ILogger LoggerInstance { get; } = Logger.LogFactory.CreateLogger("");
    public DotEnv Env { get; }
    public OTSClient Client { get; }

    public Database(IConfiguration config)
    {
        Env = new DotEnv(config);
        Client = new OTSClient(new OTSClientConfig(
            new string(Env.InVpc ? $"https://{Env.DatabaseName}.{Env.Region}.vpc.ots.aliyuncs.com" : $"https://{Env.DatabaseName}.{Env.Region}.ots.aliyuncs.com"),
            Env.EffectiveAccessKeyId,
            Env.EffectiveAccessKeySecret,
            Env.DatabaseName,
            Env.EffectiveSecurityToken
        ));
    }
}