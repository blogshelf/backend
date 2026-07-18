using backend.middleware;

namespace backend.Utils;

public static class HttpContextExtensions
{
    public static T GetBody<T>(this HttpContext ctx) where T : class
    {
        return ctx.Items[MsgPackBodyFilter<T>.ItemKey] as T ?? throw new ArgumentException("Body is null");
    }

    public static string GetRequestId(this HttpContext ctx)
    {
        return ctx.Items["RequestId"] as string ?? throw new InvalidOperationException("RequestId not set");
    }
}