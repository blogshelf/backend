using Aliyun.Credentials;
using Aliyun.Credentials.Models;

namespace backend.initialization;

public class DotEnv
{
    internal DotEnv()
    {
        if (SecurityToken is null && RoleArn is not null && RoleSessionName is not null)
            Credential = new Client(new Config
            {
                Type = "ram_role_arn",
                AccessKeyId = AccessKey,
                AccessKeySecret = SecretKey,
                RoleArn = RoleArn,
                RoleSessionName = RoleSessionName
            });
        else if (SecurityToken is not null)
            Credential = new Client(new Config
            {
                Type = "sts",
                // 从环境变量中获取AccessKey ID值。
                AccessKeyId = AccessKey,
                // 从环境变量中获取AccessKey Secret值。
                AccessKeySecret = SecretKey,
                // 从环境变量中获取的临时SecurityToken。
                SecurityToken = SecurityToken
            });
    }

    private string AccessKey { get; } = Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_ID ") ??
                                        throw new InvalidOperationException("必须设置数据库访问密钥");

    private string SecretKey { get; } = Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_SECRET ") ??
                                        throw new InvalidOperationException("必须设置数据库访问密钥");

    private string? SecurityToken { get; } = Environment.GetEnvironmentVariable("ALIBABA_CLOUD_SECURITY_TOKEN");
    private string? RoleArn { get; } = Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ROLE_ARN");
    private string? RoleSessionName { get; } = Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ROLE_SESSION_NAME");
    public string Region { get; } = Environment.GetEnvironmentVariable("REGION") ?? "cn-hongkong";
    public bool InVpc { get; } = bool.Parse(Environment.GetEnvironmentVariable("IN_VPC") ?? "false");
    internal Client? Credential { get; set; }
    
    internal string DatabaseName { get; } = Environment.GetEnvironmentVariable("DATABSE_NAME") ?? throw new InvalidOperationException("必须设置数据库名称");
}