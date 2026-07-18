using System.Security.Cryptography;
using Aliyun.Credentials;
using Aliyun.Credentials.Models;

namespace backend.initialization;

public class DotEnv
{
    private readonly IConfiguration _config;

    public DotEnv(IConfiguration config)
    {
        _config = config;
        CommitSha = GetOptional("HEAD");

        var credConfig = new Config
        {
            AccessKeyId = AccessKey,
            AccessKeySecret = SecretKey
        };

        if (SecurityToken is not null)
        {
            credConfig.Type = "sts";
            credConfig.SecurityToken = SecurityToken;
        }
        else if (RoleArn is not null)
        {
            credConfig.Type = "ram_role_arn";
            credConfig.RoleArn = RoleArn;
            credConfig.RoleSessionName = RoleSessionName;
        }

        if (credConfig.Type is not null)
            Credential = new Client(credConfig);
    }

    private string AccessKey => Get("ALIBABA_CLOUD_ACCESS_KEY_ID");
    private string SecretKey => Get("ALIBABA_CLOUD_ACCESS_KEY_SECRET");

    private string? SecurityToken => GetOptional("ALIBABA_CLOUD_SECURITY_TOKEN");
    private string? RoleArn => GetOptional("ALIBABA_CLOUD_ROLE_ARN");
    private string RoleSessionName => GetOptional("ALIBABA_CLOUD_ROLE_SESSION_NAME") ?? "blogshelf-backend";

    public string Region => GetOptional("REGION") ?? "cn-hongkong";
    public bool InVpc => bool.Parse(GetOptional("IN_VPC") ?? "false");
    private Client? Credential { get; }

    internal string EffectiveAccessKeyId => Credential?.GetCredential()?.AccessKeyId ?? AccessKey;
    internal string EffectiveAccessKeySecret => Credential?.GetCredential()?.AccessKeySecret ?? SecretKey;
    internal string? EffectiveSecurityToken => Credential?.GetCredential()?.SecurityToken ?? SecurityToken;

    internal string DatabaseName => Get("DATABASE_NAME");
#if DEBUG
    internal string? UserIdPrivateKey => GetOptional("OWNER_PRIVATE_KEY");
#endif
    internal string UserIdPublicKey => Get("OWNER_PUBLIC_KEY");

    internal ECDiffieHellmanPublicKey EcdhPublicKey
    {
        get
        {
            if (field is not null) return field;
            using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            ecdh.ImportFromPem(UserIdPublicKey);
            return ecdh.PublicKey;
        }
    }

    private string? CommitSha { get; }
    public string TablePostFix { get; set; } = "";

    public string EmailSecret => Get("EMAIL_SECRET");

    internal string SmtpHost => GetOptional("SMTP_HOST") ?? "";
    internal int SmtpPort => int.Parse(GetOptional("SMTP_PORT") ?? "465");
    internal string SmtpUser => GetOptional("SMTP_USER") ?? "";
    internal string SmtpPass => GetOptional("SMTP_PASS") ?? "";
    internal bool SmtpSsl => bool.Parse(GetOptional("SMTP_SSL") ?? "true");
    internal string SmtpFrom => GetOptional("SMTP_FROM") ?? SmtpUser;

    private string Get(string key)
    {
        return _config.GetSection(key).Value ?? Environment.GetEnvironmentVariable(key) ??
            throw new InvalidOperationException($"必须设置 {key}");
    }

    private string? GetOptional(string key)
    {
        return _config.GetSection(key).Value ?? Environment.GetEnvironmentVariable(key);
    }
}