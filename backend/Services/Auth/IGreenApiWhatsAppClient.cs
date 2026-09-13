namespace Backend.Services.Auth;

/// <summary>
/// GreenAPI <c>checkWhatsapp</c> HTTP call. Does not persist and does not require a live account
/// in tests — mock this interface.
/// </summary>
public interface IGreenApiWhatsAppClient
{
    /// <summary>
    /// Checks one phone via GreenAPI. Does not write <c>OnlineVoter</c>.
    /// When GreenAPI is not configured, <see cref="GreenApiWhatsAppCheckResult.ProviderCalled"/>
    /// is false and no HTTP request is made.
    /// </summary>
    Task<GreenApiWhatsAppCheckResult> CheckWhatsAppAsync(
        string phone,
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
