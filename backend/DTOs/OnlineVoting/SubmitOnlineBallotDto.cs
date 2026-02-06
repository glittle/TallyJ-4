namespace TallyJ4.DTOs.OnlineVoting;

/// <summary>
/// Data transfer object for submitting an online ballot.
/// </summary>
public class SubmitOnlineBallotDto
{
    /// <summary>
    /// The unique identifier of the election.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The voter's unique identifier.
    /// </summary>
    public string VoterId { get; set; } = null!;

    /// <summary>
    /// The list of votes on the ballot.
    /// </summary>
    public List<OnlineVoteDto> Votes { get; set; } = new();
}

/// <summary>
/// Data transfer object representing a single vote in an online ballot.
/// </summary>
public class OnlineVoteDto
{
    /// <summary>
    /// The unique identifier of the person being voted for.
    /// </summary>
    public Guid? PersonGuid { get; set; }

    /// <summary>
    /// The name of the person being voted for (if not a predefined candidate).
    /// </summary>
    public string? VoteName { get; set; }

    /// <summary>
    /// The position of this vote on the ballot.
    /// </summary>
    public int PositionOnBallot { get; set; }
}
