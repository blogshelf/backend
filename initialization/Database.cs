using Aliyun.OTS;

namespace backend.initialization;

public static class Database
{
    public static ILogger LoggerInstance { get; } = Logger.LogFactory.CreateLogger("");
    private static DotEnv Env { get; } = new DotEnv();
    internal static OTSClient Client { get; } = new OTSClient(new OTSClientConfig(
        new string(Env.InVpc ? $"https://{Env.Region}.vpc.ots.aliyuncs.com" : $"https://{Env.Region}.ots.aliyuncs.com"),
        Env.Credential?.GetCredential().AccessKeyId,
        Env.Credential?.GetCredential().AccessKeySecret,
        Env.DatabaseName
        ));
}