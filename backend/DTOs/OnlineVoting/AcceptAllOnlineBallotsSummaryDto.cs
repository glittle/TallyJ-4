namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Counts shown before a teller confirms Accept-all.
/// </summary>
public class AcceptAllOnlineBallotsSummaryDto
{
    /// <summary>
    /// Online ballots with status Submitted that this run would accept.
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// Online ballots already processed into regular ballots.
    /// </summary>
    public int ProcessedCount { get; set; }
}
