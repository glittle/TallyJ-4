using Backend.DTOs.People;

namespace Backend.Services;

/// <summary>
/// Head-teller WhatsApp notify queue (v3 <c>SendHeadTellerMessage</c> / <c>AbortQueue</c>).
/// One active run per election. Production POSTs GreenAPI <c>sendMessage</c>.
/// </summary>
public interface IWhatsAppNotifyQueue
{
    /// <summary>
    /// Classify selected people and start a background sequential send for known-OK
    /// phone P rows. Returns immediately with skip counts and a queue token.
    /// </summary>
    Task<WhatsAppNotifyStatusDto> StartAsync(
        Guid electionGuid,
        IReadOnlyList<Guid> personGuids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resend people whose last WhatsApp announcement failed.
    /// Each retry reserves a paid-send slot. A blocked retry does not call GreenAPI.
    /// </summary>
    Task<WhatsAppNotifyStatusDto> RetryFailedAsync(
        Guid electionGuid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Abort remaining sends for this election (or the given token).
    /// Already-sent messages stay sent.
    /// </summary>
    WhatsAppNotifyStatusDto? Abort(Guid electionGuid, string? queueToken = null);

    /// <summary>
    /// Latest run for this election, or the run matching <paramref name="queueToken"/>.
    /// </summary>
    WhatsAppNotifyStatusDto? GetStatus(Guid electionGuid, string? queueToken = null);
}
