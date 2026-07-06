using System.Diagnostics;
using Aliyun.OTS;
using Ardalis.GuardClauses;
using NanoidDotNet;
using ListTableRequest = Aliyun.OTS.Request.ListTableRequest;

namespace backend.initialization;

public abstract class PreExec
{
    public DotEnv Env { get; } = new();
    public static OTSClient? Client { get; } = Database.Client;

    public static void Run()
    {
        try
        {
#if DEBUG
            Debug.Assert(Client != null, nameof(Client) + " != null");
#endif
            var response = Client.ListTable(new ListTableRequest());
            Guard.Against.NullOrEmpty(response.TableNames, nameof(response.TableNames));
        }
        catch (Exception ex)
        {
            Database.LoggerInstance.LogError(Nanoid.Generate(Nanoid.Alphabets.HexadecimalUppercase, 32), ex);
            throw new IOException("数据库探测异常", ex);
        }
    }
}