namespace Backend.Services;

/// <summary>
/// Emails the super admin and records a Sentry warning for paid-send abuse events.
/// The same key is not sent again until the throttle window passes.
/// </summary>
public interface IAbuseAlertService
{
    /// <summary>
    /// Alerts that an election or owner hit a paid-send cap.
    /// </summary>
    Task NotifyCapHitAsync(AbuseCapHitAlert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Alerts that an owner made their first SMS, voice, or WhatsApp send.
    /// </summary>
    Task NotifyFirstPaidSendAsync(AbuseFirstPaidSendAlert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Alerts that an election was flagged. Online voting and every login code are stopped.
    /// </summary>
    Task NotifyElectionFlaggedAsync(AbuseElectionFlaggedAlert alert, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cap-hit details included in the super admin email. Destinations are not included.
/// </summary>
public sealed record AbuseCapHitAlert(
    string AlertKey,
    string Scope,
    Guid? ElectionGuid,
    string? ElectionName,
    Guid? OwnerUserId,
    int Used,
    int Cap);

/// <summary>
/// First paid-send details. The destination is already masked.
/// </summary>
public sealed record AbuseFirstPaidSendAlert(
    Guid OwnerUserId,
    Guid ElectionGuid,
    string? ElectionName,
    string Channel,
    string MaskedDestination);

/// <summary>
/// One flagged voter-list row included in the flag alert.
/// </summary>
public sealed record AbuseFlaggedRow(int? RowNumber, string MaskedValue, string Reason);

/// <summary>
/// Flag alert. Online voting and every login code are stopped until a super admin clears the flag.
/// </summary>
public sealed record AbuseElectionFlaggedAlert(
    Guid ElectionGuid,
    string? ElectionName,
    int FlaggedEntryCount,
    IReadOnlyList<AbuseFlaggedRow> Rows);
