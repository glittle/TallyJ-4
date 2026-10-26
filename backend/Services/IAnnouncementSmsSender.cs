namespace Backend.Services;

/// <summary>
/// Twilio SMS for a head-teller announcement body. Not the verification-code sender.
/// </summary>
public interface IAnnouncementSmsSender
{
    /// <summary>
    /// True when Twilio account, token, and from-number are set and are not placeholders.
    /// </summary>
    bool IsConfigured();

    /// <summary>
    /// Sends <paramref name="message"/>. On success, writes an <c>SmsLog</c> when Twilio returns a SID.
    /// A failed send does not write <c>SmsLog</c>.
    /// </summary>
    Task<AnnouncementProviderResult> SendAsync(
        string phone,
        string message,
        Guid electionGuid,
        Guid personGuid,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// SMTP email for a head-teller announcement body.
/// </summary>
public interface IAnnouncementEmailSender
{
    /// <summary>
    /// True when the SMTP host is set and is not a placeholder.
    /// </summary>
    bool IsConfigured();

    /// <summary>
    /// Sends <paramref name="body"/> as plain text. Does not log the recipient address.
    /// </summary>
    Task<AnnouncementProviderResult> SendAsync(
        string toAddress,
        string? fromAddress,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Provider outcome for one announcement. <see cref="Error"/> is already safe to show.
/// </summary>
public readonly record struct AnnouncementProviderResult(
    bool Sent,
    bool ProviderCalled,
    string? Error)
{
    public static AnnouncementProviderResult NotConfigured() =>
        new(false, false, "not-configured");

    public static AnnouncementProviderResult Failed(string? error) =>
        new(false, true, Helpers.AnnouncementErrorText.Sanitize(error) ?? "send-failed");

    public static AnnouncementProviderResult Succeeded() =>
        new(true, true, null);
}
