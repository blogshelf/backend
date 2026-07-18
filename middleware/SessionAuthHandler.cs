using System.Security.Claims;
using System.Text.Encodings.Web;
using Aliyun.OTS;
using backend.database;
using backend.Utils;
using MessagePack;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace backend.middleware;

public class SessionAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader)
            || !authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var token = authHeader.ToString()["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return Task.FromResult(AuthenticateResult.NoResult());

        byte[] envelopeBytes;
        try
        {
            envelopeBytes = Convert.FromBase64String(token);
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid token format"));
        }

        BearerEnvelope envelope;
        try
        {
            envelope = MessagePackSerializer.Deserialize<BearerEnvelope>(envelopeBytes);
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid token"));
        }

        var client = Context.RequestServices.GetRequiredService<OTSClient>();

        Session session;
        try
        {
            session = new Session
            {
                UserId = envelope.UserId,
                SessionId = envelope.SessionId
            }.GetSingle(client);
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Session not found"));
        }

        if (!session.SrpState || session.Seed.Length == 0)
            return Task.FromResult(AuthenticateResult.Fail("Session not authenticated"));

        if (session.ExpiresAt > 0 && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > session.ExpiresAt)
            return Task.FromResult(AuthenticateResult.Fail("Session expired"));

        if (!RequestTokenHelper.VerifyToken(RequestTokenHelper.DeriveRequestKey(session.Seed), envelope.Proof,
                (long)(DateTimeOffset.UtcNow - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalMinutes))
            return Task.FromResult(AuthenticateResult.Fail("Token invalid or expired"));

        var claims = new[]
        {
            new Claim("userId", Convert.ToBase64String(envelope.UserId)),
            new Claim("sessionId", Convert.ToBase64String(envelope.SessionId))
        };
        var identity = new ClaimsIdentity(claims, "Session");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Session");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}