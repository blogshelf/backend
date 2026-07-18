using backend.initialization;
using MailKit.Security;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace backend.services;

public class NoopEmailSender(ILogger<NoopEmailSender> log) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        log.LogInformation("[NoopEmail] To: {To}\nSubject: {Subject}\nBody: {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}

public class SmtpEmailSender(ILogger<SmtpEmailSender> log, DotEnv env) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var message = new MimeMessage
        {
            Subject = subject,
            Body = new TextPart("html") { Text = body }
        };
        message.From.Add(MailboxAddress.Parse(env.SmtpFrom));
        message.To.Add(MailboxAddress.Parse(to));

        using var client = new SmtpClient();
        await client.ConnectAsync(env.SmtpHost, env.SmtpPort, env.SmtpSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(env.SmtpUser, env.SmtpPass, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}