using System.Diagnostics.CodeAnalysis;
using MessagePack;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace backend.handler;

public class MsgPackInputFormatter : InputFormatter
{
    public MsgPackInputFormatter()
    {
        SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/vnd.msgpack"));
        SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/vnd.msgpack+x-msgpack"));
        SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/x-msgpack"));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2046:RequiresUnreferencedCodeAttribute mismatch",
        Justification = "All MessagePack model types are annotated with [MessagePackObject].")]
    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
    {
        using var ms = new MemoryStream();
        await context.HttpContext.Request.Body.CopyToAsync(ms);
        var obj = MessagePackSerializer.Deserialize(context.ModelType, ms.ToArray());
        return await InputFormatterResult.SuccessAsync(obj);
    }

    protected override bool CanReadType(Type type)
    {
        return true;
    }
}