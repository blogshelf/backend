using Aliyun.OTS;

namespace backend.initialization;

public class Database
{
    public Database(IConfiguration config)
    {
        Env = new DotEnv(config);
        Client = new OTSClient(new OTSClientConfig(
            new string(Env.InVpc
                ? $"https://{Env.DatabaseName}.{Env.Region}.vpc.ots.aliyuncs.com"
                : $"https://{Env.DatabaseName}.{Env.Region}.tablestore.aliyuncs.com"),
            Env.EffectiveAccessKeyId,
            Env.EffectiveAccessKeySecret,
            Env.DatabaseName,
            Env.EffectiveSecurityToken
        ));
    }

    public static ILogger LoggerInstance { get; } = Logger.LogFactory.CreateLogger("");
    private DotEnv Env { get; }
    public OTSClient Client { get; }
}