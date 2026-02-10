namespace TallyJ4.DTOs.Votes;

/// <summary>
/// Data transfer object representing a vote in an election.
/// Contains vote details including candidate information and ballot context.
/// </summary>
public class VoteDto
{
    /// <summary>
    /// The unique row identifier for this vote.
    /// </summary>
    public int RowId { get; set; }

    /// <summary>
    /// The unique identifier of the ballot this vote belongs to.
    /// </summary>
    public Guid BallotGuid { get; set; }

    /// <summary>
    /// The position of this vote on the ballot (1-based indexing).
    /// </summary>
    public int PositionOnBallot { get; set; }

    /// <summary>
    /// The unique identifier of the person (candidate) being voted for.
    /// Can be null for certain types of votes.
    /// </summary>
    public Guid? PersonGuid { get; set; }

    /// <summary>
    /// The full name of the person being voted for.
    /// </summary>
    public string? PersonFullName { get; set; }

    /// <summary>
    /// The status code of the vote (e.g., "ok", "spoiled").
    /// </summary>
    public string StatusCode { get; set; } = null!;

    /// <summary>
    /// Combined information about the person for display purposes.
    /// </summary>
    public string? PersonCombinedInfo { get; set; }

    /// <summary>
    /// Raw data from online voting systems.
    /// </summary>
    public string? OnlineVoteRaw { get; set; }
}
