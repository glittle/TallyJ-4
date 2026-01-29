namespace TallyJ4.DTOs.SignalR;

/// <summary>
/// Data transfer object for real-time tally progress updates via SignalR.
/// Contains information about the current state of election result calculation.
/// </summary>
public class TallyProgressDto
{
    /// <summary>
    /// The unique identifier of the election being tallied.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The total number of ballots in the election.
    /// </summary>
    public int TotalBallots { get; set; }

    /// <summary>
    /// The number of ballots that have been processed so far.
    /// </summary>
    public int ProcessedBallots { get; set; }

    /// <summary>
    /// The total number of votes expected in the election.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// A status message describing the current tally operation.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The percentage of completion (0-100).
    /// </summary>
    public int PercentComplete { get; set; }

    /// <summary>
    /// Indicates whether the tally operation has completed.
    /// </summary>
    public bool IsComplete { get; set; }
}
