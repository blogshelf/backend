namespace backend.Utils;

internal static partial class DisposableDomains
{
    public static bool IsDisposable(string domain)
    {
        return Domains.Contains(domain);
    }
}