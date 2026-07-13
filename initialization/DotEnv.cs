using Aliyun.Credentials;
using Aliyun.Credentials.Models;
using Microsoft.Extensions.Configuration;

namespace backend.initialization;

public class DotEnv
{
    private readonly IConfiguration _config;

    internal DotEnv(IConfiguration config)
    {
        _config = config;

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
                AccessKeyId = AccessKey,
                AccessKeySecret = SecretKey,
                SecurityToken = SecurityToken
            });
    }

    private string Get(string key) =>
        _config[key] ?? Environment.GetEnvironmentVariable(key) ??
        throw new InvalidOperationException($"必须设置 {key}");

    private string? GetOptional(string key) =>
        _config[key] ?? Environment.GetEnvironmentVariable(key);

    private string AccessKey => Get("ALIBABA_CLOUD_ACCESS_KEY_ID");
    private string SecretKey => Get("ALIBABA_CLOUD_ACCESS_KEY_SECRET");

    private string? SecurityToken => GetOptional("ALIBABA_CLOUD_SECURITY_TOKEN");
    private string? RoleArn => GetOptional("ALIBABA_CLOUD_ROLE_ARN");
    private string? RoleSessionName => GetOptional("ALIBABA_CLOUD_ROLE_SESSION_NAME");
    public string Region => GetOptional("REGION") ?? "cn-hongkong";
    public bool InVpc => bool.Parse(GetOptional("IN_VPC") ?? "false");
    internal Client? Credential { get; set; }

    internal string EffectiveAccessKeyId => Credential?.GetCredential()?.AccessKeyId ?? AccessKey;
    internal string EffectiveAccessKeySecret => Credential?.GetCredential()?.AccessKeySecret ?? SecretKey;
    internal string? EffectiveSecurityToken => Credential?.GetCredential()?.SecurityToken ?? SecurityToken;

    internal string DatabaseName => Get("DATABSE_NAME");
}