using Backend.DTOs.People;

namespace Backend.Services;

/// <summary>
/// Bulk email and SMS announcements. One active run per election and channel.
/// SMS reserves a paid-send slot before Twilio is called. Email does not.
/// A freeze or a flagged election blocks both. A retry uses the same path.
/// </summary>
public interface IAnnouncementQueue
{
    /// <summary>
    /// Classify selected people and start a background send. Returns immediately.
    /// </summary>
    Task<AnnouncementStatusDto> StartAsync(
        Guid electionGuid,
        string channel,
        IReadOnlyList<Guid> personGuids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resend people in this election whose last attempt on <paramref name="channel"/> failed.
    /// Each SMS retry reserves another paid-send slot. A blocked retry does not call the provider.
    /// </summary>
    Task<AnnouncementStatusDto> RetryFailedAsync(
        Guid electionGuid,
        string channel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Abort remaining sends for this channel. Already-sent messages stay sent.
    /// </summary>
    AnnouncementStatusDto? Abort(Guid electionGuid, string channel, string? queueToken = null);

    /// <summary>
    /// Latest run for this election and channel, or the run matching <paramref name="queueToken"/>.
    /// </summary>
    AnnouncementStatusDto? GetStatus(Guid electionGuid, string channel, string? queueToken = null);
}
