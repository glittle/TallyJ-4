namespace TallyJ4.DTOs.Votes;

/// <summary>
/// Data transfer object for creating a new vote.
/// Contains the essential information needed to record a vote on a ballot.
/// </summary>
public class CreateVoteDto
{
    /// <summary>
    /// The unique identifier of the ballot this vote belongs to.
    /// </summary>
    public Guid BallotGuid { get; set; }

    /// <summary>
    /// The unique identifier of the person (candidate) being voted for.
    /// Can be null for certain types of votes.
    /// </summary>
    public Guid? PersonGuid { get; set; }

    /// <summary>
    /// The position of this vote on the ballot (1-based indexing).
    /// </summary>
    public int PositionOnBallot { get; set; }

    /// <summary>
    /// The status code of the vote (e.g., "ok", "spoiled").
    /// </summary>
    public string StatusCode { get; set; } = "ok";
}
