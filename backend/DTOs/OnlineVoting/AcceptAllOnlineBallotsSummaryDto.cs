namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Counts shown before a teller confirms Accept-all.
/// </summary>
public class AcceptAllOnlineBallotsSummaryDto
{
    /// <summary>
    /// Online ballots this run would accept: <c>Submitted</c> + <c>Processing</c>.
    /// Same set as monitor pending. Count only; no person identity.
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// Online ballots already accepted (<c>Processed</c>) into regular ballots.
    /// Count only; no person identity.
    /// </summary>
    public int ProcessedCount { get; set; }
}
