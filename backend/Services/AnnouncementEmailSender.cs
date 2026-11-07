using Backend.Helpers;
using Backend.Services.Auth;
using MailKit.Net.Smtp;
using MimeKit;

namespace Backend.Services;

/// <summary>
/// Plain-text announcement email. Does not log the recipient address.
/// </summary>
public class AnnouncementEmailSender : IAnnouncementEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AnnouncementEmailSender> _logger;

    public AnnouncementEmailSender(
        IConfiguration configuration,
        IEmailSender emailSender,
        ILogger<AnnouncementEmailSender> logger)
    {
        _configuration = configuration;
        _emailSender = emailSender;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsConfigured() => !IsPlaceholder(_configuration["Email:SmtpHost"]);

    /// <inheritdoc />
    public async Task<AnnouncementProviderResult> SendAsync(
        string toAddress,
        string? replyToAddress,
        string? fromDisplayName,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (!MailboxAddress.TryParse(toAddress, out var to))
        {
            return AnnouncementProviderResult.Failed("invalid-email");
        }

        if (!IsConfigured())
        {
            return AnnouncementProviderResult.NotConfigured();
        }

        var from = ResolveSystemFrom(fromDisplayName);
        if (from == null)
        {
            return AnnouncementProviderResult.Failed("not-configured");
        }

        var message = new MimeMessage();
        message.From.Add(from);
        message.To.Add(to);
        var replyTo = ResolveReplyTo(replyToAddress, fromDisplayName);
        if (replyTo != null)
        {
            message.ReplyTo.Add(replyTo);
        }

        message.Subject = string.IsNullOrWhiteSpace(subject) ? "Announcement" : subject.Trim();
        message.Body = new TextPart("plain") { Text = body };

        try
        {
            await _emailSender.SendAsync(message);
            return AnnouncementProviderResult.Succeeded();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Do not pass ex. SMTP text usually repeats the recipient address.
            _logger.LogWarning(
                "Announcement email failed ({ExceptionType}) smtp {SmtpStatus}",
                ex.GetType().Name,
                SmtpStatus(ex));
            return AnnouncementProviderResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// System mailbox only. An election address is Reply-To, not From, so SPF and DMARC stay on our domain.
    /// </summary>
    private MailboxAddress? ResolveSystemFrom(string? displayName)
    {
        var address = _configuration["Email:FromAddress"];
        if (IsPlaceholder(address) || !MailboxAddress.TryParse(address, out var mailbox))
        {
            return null;
        }

        var name = displayName?.Trim();
        if (IsPlaceholder(name))
        {
            name = _configuration["Email:FromName"];
        }

        if (IsPlaceholder(name))
        {
            name = "TallyJ";
        }

        return new MailboxAddress(name, mailbox.Address);
    }

    private static MailboxAddress? ResolveReplyTo(string? replyToAddress, string? displayName)
    {
        if (IsPlaceholder(replyToAddress) || !MailboxAddress.TryParse(replyToAddress, out var mailbox))
        {
            return null;
        }

        var name = displayName?.Trim();
        return IsPlaceholder(name)
            ? new MailboxAddress(mailbox.Address, mailbox.Address)
            : new MailboxAddress(name, mailbox.Address);
    }

    private static string SmtpStatus(Exception ex) =>
        ex is SmtpCommandException smtp ? ((int)smtp.StatusCode).ToString() : "none";

    private static bool IsPlaceholder(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.TrimStart().StartsWith('<');
}
