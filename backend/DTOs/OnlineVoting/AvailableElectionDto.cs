namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Data transfer object for an election available to a voter.
/// </summary>
public class AvailableElectionDto
{
    /// <summary>
    /// The election GUID.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the election.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The election convenor.
    /// </summary>
    public string? Convenor { get; set; }

    /// <summary>
    /// When online voting opens.
    /// </summary>
    public DateTimeOffset? OnlineWhenOpen { get; set; }

    /// <summary>
    /// When online voting closes.
    /// </summary>
    public DateTimeOffset? OnlineWhenClose { get; set; }

    /// <summary>
    /// Whether the close time is an estimate.
    /// </summary>
    public bool OnlineCloseIsEstimate { get; set; }

    /// <summary>
    /// The date of the election.
    /// </summary>
    public DateTimeOffset? DateOfElection { get; set; }

    /// <summary>
    /// Whether online voting is currently open.
    /// </summary>
    public bool IsOpen { get; set; }

    /// <summary>
    /// Whether online voting is configured at all for this election.
    /// </summary>
    public bool HasOnlineVoting { get; set; }

    /// <summary>
    /// Whether the voter has already voted in this election.
    /// </summary>
    public bool HasVoted { get; set; }

    /// <summary>
    /// The voter's registered name in this election.
    /// </summary>
    public string? VoterName { get; set; }

    /// <summary>
    /// The ballot status for this voter (e.g. Draft, Processed).
    /// </summary>
    public string? BallotStatus { get; set; }

    /// <summary>
    /// When the ballot was submitted/processed.
    /// </summary>
    public DateTimeOffset? WhenBallotStatus { get; set; }
}
