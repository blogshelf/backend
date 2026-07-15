using MessagePack;

namespace backend.middleware;

public class MsgPackBodyFilter<T> : IEndpointFilter
{
    public const string ItemKey = "MsgPackBody";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        using var ms = new MemoryStream();
        await context.HttpContext.Request.Body.CopyToAsync(ms);
        context.HttpContext.Items[ItemKey] = MessagePackSerializer.Deserialize<T>(ms.ToArray());
        return await next(context);
    }
}
