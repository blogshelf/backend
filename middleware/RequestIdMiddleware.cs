using backend.Utils;

namespace backend.middleware;

public class RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = IdGen.New();
        context.Items["RequestId"] = requestId;

        using (logger.BeginScope(new Dictionary<string, object?> { ["RequestId"] = requestId }))
        {
            logger.LogInformation("[{Method}] {Path}", context.Request.Method, context.Request.Path);
            try
            {
                await next(context);
            }
            catch (Exception e)
            {
                logger.LogError(e, "[{Method}] {Path} unhandled exception", context.Request.Method, context.Request.Path);
                throw;
            }
            finally
            {
                logger.LogInformation("[{StatusCode}] {Method} {Path}", context.Response.StatusCode, context.Request.Method, context.Request.Path);
            }
        }
    }
}
