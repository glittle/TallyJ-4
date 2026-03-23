using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Backend.Application.Services.Auth;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(MimeMessage message)
    {
        var smtpHost = _configuration["Email:SmtpHost"];
        if (string.IsNullOrWhiteSpace(smtpHost) || smtpHost.StartsWith('<'))
        {
            _logger.LogWarning("Email not configured; skipping send to {To}", message.To);
            return;
        }

        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var useSsl = bool.Parse(_configuration["Email:UseSsl"] ?? "true");

        using var client = new SmtpClient();
        await client.ConnectAsync(smtpHost, smtpPort,
            useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            await client.AuthenticateAsync(username, password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
