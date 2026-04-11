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
}



