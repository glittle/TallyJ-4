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
    /// No owner or admin is approved for paid sends.
    /// </summary>
    public const string NotApproved = "not-approved";

    /// <summary>
    /// The election allowance is used up.
    /// </summary>
    public const string ElectionCap = "election-cap";

    /// <summary>
    /// Every approved owner is at today's cap.
    /// </summary>
    public const string OwnerDailyCap = "owner-daily-cap";

    /// <summary>
    /// A super admin froze this election or every owner on it.
    /// </summary>
    public const string Frozen = "frozen";

    /// <summary>
    /// The voter list flagged this election.
    /// </summary>
    public const string Flagged = "flagged";
}
