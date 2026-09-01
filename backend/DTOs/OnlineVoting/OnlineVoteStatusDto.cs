namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Data transfer object representing the voting status of an online voter.
/// </summary>
public class OnlineVoteStatusDto
{
    /// <summary>
    /// Indicates whether the voter has already voted.
    /// </summary>
    public bool HasVoted { get; set; }

    /// <summary>
    /// The timestamp when the vote was submitted.
    /// </summary>
    public DateTimeOffset? WhenSubmitted { get; set; }

    /// <summary>
    /// A message about the voting status.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Prior votes on the submitted ballot (for editing).
    /// </summary>
    public List<OnlineVoteDto> PriorVotes { get; set; } = new();

    /// <summary>
    /// Names the voter added to their personal pool.
    /// </summary>
    public List<OnlinePoolEntryDto> ListPool { get; set; } = new();

    /// <summary>
    /// Whether the voter opted in to a ballot-processed notification.
    /// </summary>
    public bool NotifyWhenProcessed { get; set; }

    /// <summary>
    /// False when Accept-all has claimed the row (Processing), finished it
    /// (Processed), or a legacy submit-creates-ballot row still has BallotGuid.
    /// True when there is no online row yet, or the row is still Submitted
    /// without BallotGuid. Same rules as
    /// <c>OnlineVotingService.CannotChangeOnlineVote</c>.
    /// </summary>
    public bool CanChangeVote { get; set; }
}



