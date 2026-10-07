namespace Backend.Services;

/// <summary>
/// Read-only paid-channel state shown to the election owner.
/// </summary>
public interface IPaidChannelStatusService
{
    /// <summary>
    /// Fills the paid-channel fields on an election the owner is editing.
    /// </summary>
    Task ApplyAsync(Guid electionGuid, DTOs.Elections.ElectionDto dto, CancellationToken cancellationToken = default);
}

/// <summary>
/// Why SMS, voice, and WhatsApp are unavailable for one election.
/// </summary>
public static class PaidChannelStatus
{
    /// <summary>
    /// An owner or admin is not approved for paid sends, or the election has none.
    /// </summary>
    public const string NotApproved = "not-approved";

    /// <summary>
    /// The election allowance is used up.
    /// </summary>
    public const string ElectionCap = "election-cap";

    /// <summary>
    /// Every owner and admin is at today's cap.
    /// </summary>
    public const string OwnerDailyCap = "owner-daily-cap";

    /// <summary>
    /// A super admin froze this election or an owner or admin on it.
    /// </summary>
    public const string Frozen = "frozen";

    /// <summary>
    /// Online voting and every login code are stopped.
    /// </summary>
    public const string Flagged = "flagged";
}
