using System.Diagnostics;
using Aliyun.OTS;
using Ardalis.GuardClauses;
using NanoidDotNet;
using ListTableRequest = Aliyun.OTS.Request.ListTableRequest;

namespace backend.initialization;

public abstract class PreExec
{
    public static void Run(OTSClient client)
    {
        try
        {
#if DEBUG
            Debug.Assert(client != null, nameof(client) + " != null");
#endif
            var response = client.ListTable(new ListTableRequest());
            Guard.Against.NullOrEmpty(response.TableNames, nameof(response.TableNames));
        }
        catch (Exception ex)
        {
            Database.LoggerInstance.LogError(Nanoid.Generate(Nanoid.Alphabets.HexadecimalUppercase, 32), ex);
            throw new IOException("数据库探测异常", ex);
        }
    }
}