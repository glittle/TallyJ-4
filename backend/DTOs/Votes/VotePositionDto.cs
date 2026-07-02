namespace Backend.DTOs.Votes;

/// <summary>
/// Lightweight vote identity and ballot position returned after delete or reorder mutations.
/// </summary>
public class VotePositionDto
{
    /// <summary>
    /// The unique row identifier for this vote.
    /// </summary>
    public int RowId { get; set; }

    /// <summary>
    /// The position of this vote on the ballot (1-based indexing).
    /// </summary>
    public int PositionOnBallot { get; set; }
}