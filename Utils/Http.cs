using MessagePack;

namespace backend.Utils;

public static class Http
{
    private const string MsgPackContentType = "application/vnd.msgpack+x-msgpack";

    public static IResult MsgPack<T>(T data, int statusCode = StatusCodes.Status200OK)
    {
        return new MsgPackResult<T>(data, statusCode);
    }

    private sealed class MsgPackResult<T>(T data, int statusCode) : IResult
    {
        public async Task ExecuteAsync(HttpContext context)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = MsgPackContentType;
            await context.Response.Body.WriteAsync(MessagePackSerializer.Serialize(data));
        }
    }
}