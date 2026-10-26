using Backend.Helpers;

namespace Backend.Services.Auth;

/// <summary>
/// GreenAPI <c>checkWhatsapp</c> / <c>sendMessage</c> HTTP calls. Does not persist and
/// does not require a live account in tests — mock this interface.
/// </summary>
public interface IGreenApiWhatsAppClient
{
    /// <summary>
    /// True when IdInstance and ApiToken are present and not placeholders.
    /// Does not call HTTP.
    /// </summary>
    bool IsConfigured();

    /// <summary>
    /// Checks one phone via GreenAPI. Does not write <c>OnlineVoter</c>.
    /// When GreenAPI is not configured, <see cref="GreenApiWhatsAppCheckResult.ProviderCalled"/>
    /// is false and no HTTP request is made.
    /// </summary>
    Task<GreenApiWhatsAppCheckResult> CheckWhatsAppAsync(
        string phone,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// GreenAPI <c>sendMessage</c> for a head-teller notify body.
    /// Verify-code WhatsApp still uses <c>PaidVerificationSender</c>'s own send path.
    /// When GreenAPI is not configured, <see cref="GreenApiWhatsAppSendResult.ProviderCalled"/>
    /// is false and no HTTP request is made.
    /// </summary>
    Task<GreenApiWhatsAppSendResult> SendMessageAsync(
        string phone,
        string message,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a GreenAPI presence check. <see cref="Status"/> is
/// <c>OK</c>, <c>no-wa</c>, or <c>check-failed</c>.
/// </summary>
public readonly record struct GreenApiWhatsAppCheckResult(string Status, bool ProviderCalled)
{
    public static GreenApiWhatsAppCheckResult NotConfigured() =>
        new(Helpers.OnlineVoterWhatsAppStatus.CheckFailed, ProviderCalled: false);

    public static GreenApiWhatsAppCheckResult FromProvider(string status) =>
        new(status, ProviderCalled: true);
}

/// <summary>
/// Result of a GreenAPI <c>sendMessage</c> call.
/// </summary>
public readonly record struct GreenApiWhatsAppSendResult(
    bool Sent,
    bool ProviderCalled,
    string? MessageId,
    string? Error = null)
{
    public static GreenApiWhatsAppSendResult NotConfigured() =>
        new(false, ProviderCalled: false, null);

    /// <summary>
    /// Provider was called and did not accept the message.
    /// <paramref name="error"/> is sanitized before it is stored.
    /// </summary>
    public static GreenApiWhatsAppSendResult Failed(string? error = null) =>
        new(false, ProviderCalled: true, null, AnnouncementErrorText.Sanitize(error) ?? "send-failed");

    public static GreenApiWhatsAppSendResult Succeeded(string messageId) =>
        new(true, ProviderCalled: true, messageId);
}
