using System.Security.Cryptography;
using backend.initialization;
using JetBrains.Annotations;

namespace backend.middleware;

public class SignVerifyingMiddleware(RequestDelegate next)
{
    private const string SignatureVerifiedKey = "SignatureVerified";

    [UsedImplicitly]
    public async Task InvokeAsync(HttpContext context, DotEnv env)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<ReqSignAttr>() is not null
            && context.Request.Headers.TryGetValue("x-keysign", out var sigHeader))
        {
            context.Request.EnableBuffering();
            using var ms = new MemoryStream();
            await context.Request.Body.CopyToAsync(ms);
            var bodyBytes = ms.ToArray();
            context.Request.Body.Position = 0;

            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            ecdsa.ImportFromPem(env.UserIdPublicKey);
            if (!ecdsa.VerifyData(bodyBytes, Convert.FromBase64String(sigHeader!), HashAlgorithmName.SHA256))
            {
                context.Items[SignatureVerifiedKey] = false;
                return;
            }

            context.Items[SignatureVerifiedKey] = true;
        }
        await next(context);
    }
}
