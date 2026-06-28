using Backend.Enumerations;

namespace Backend.DTOs.Votes;

/// <summary>
/// Response DTO for vote create/update/delete operations, containing the ballot's current status.
/// Vote is populated for create and update; it is omitted for delete.
/// </summary>
public class VoteWithBallotStatusDto
{
    /// <summary>
    /// The vote that was created or updated. Null when a vote was deleted.
    /// </summary>
    public VoteDto? Vote { get; set; }

    /// <summary>
    /// The current status of the ballot containing this vote.
    /// </summary>
    public BallotStatus BallotStatusCode { get; set; }

    /// <summary>
    /// All votes on the ballot after the operation, in position order with contiguous positions.
    /// </summary>
    public List<VoteDto> Votes { get; set; } = new();
}
