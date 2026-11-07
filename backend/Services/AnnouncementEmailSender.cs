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
        if (TryReplyTo(replyToAddress, fromDisplayName, out var replyTo))
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
    /// Configured system mailbox only. A placeholder or missing address does not become a guessed From.
    /// The display name is the election EmailFromName, then Email:FromName, then the same "TallyJ" name other mail uses.
    /// </summary>
    private MailboxAddress? ResolveSystemFrom(string? displayName)
    {
        var address = _configuration["Email:FromAddress"];
        if (IsPlaceholder(address) || !MailboxAddress.TryParse(address, out var mailbox))
        {
            return null;
        }

        return new MailboxAddress(ResolveDisplayName(displayName), mailbox.Address);
    }

    /// <summary>
    /// Election address as Reply-To when it parses. Invalid or missing addresses are omitted.
    /// </summary>
    private static bool TryReplyTo(string? replyToAddress, string? displayName, out MailboxAddress replyTo)
    {
        replyTo = null!;
        if (IsPlaceholder(replyToAddress) || !MailboxAddress.TryParse(replyToAddress, out var mailbox))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(mailbox.Address) || !mailbox.Address.Contains('@'))
        {
            return false;
        }

        var name = string.IsNullOrWhiteSpace(displayName) ? mailbox.Address : displayName.Trim();
        replyTo = new MailboxAddress(name, mailbox.Address);
        return true;
    }

    private string ResolveDisplayName(string? electionName)
    {
        if (!IsPlaceholder(electionName))
        {
            return electionName!.Trim();
        }

        var configured = _configuration["Email:FromName"];
        if (!IsPlaceholder(configured))
        {
            return configured!.Trim();
        }

        return "TallyJ";
    }

    private static string SmtpStatus(Exception ex) =>
        ex is SmtpCommandException smtp ? ((int)smtp.StatusCode).ToString() : "none";

    private static bool IsPlaceholder(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.TrimStart().StartsWith('<');
}
