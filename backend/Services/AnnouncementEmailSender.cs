using Backend.Helpers;
using Backend.Services.Auth;
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
        string? fromAddress,
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

        var from = ResolveFrom(fromAddress);
        if (from == null)
        {
            return AnnouncementProviderResult.Failed("not-configured");
        }

        var message = new MimeMessage();
        message.From.Add(from);
        message.To.Add(to);
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
            _logger.LogWarning(ex, "Announcement email failed");
            return AnnouncementProviderResult.Failed(ex.Message);
        }
    }

    private MailboxAddress? ResolveFrom(string? electionFrom)
    {
        if (!IsPlaceholder(electionFrom) && MailboxAddress.TryParse(electionFrom, out var electionMailbox))
        {
            return electionMailbox;
        }

        var address = _configuration["Email:FromAddress"];
        if (IsPlaceholder(address) || !MailboxAddress.TryParse(address, out var mailbox))
        {
            return null;
        }

        var name = _configuration["Email:FromName"];
        if (IsPlaceholder(name))
        {
            name = "TallyJ";
        }

        return new MailboxAddress(name, mailbox.Address);
    }

    private static bool IsPlaceholder(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.TrimStart().StartsWith('<');
}
