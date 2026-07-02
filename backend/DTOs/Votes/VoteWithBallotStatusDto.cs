using Backend.Enumerations;

namespace Backend.DTOs.Votes;

/// <summary>
/// Response DTO for vote mutation operations.
/// Create and update return the affected vote; delete and reorder return authoritative position mappings.
/// </summary>
public class VoteWithBallotStatusDto
{
    /// <summary>
    /// The vote that was created or updated. Omitted for delete and reorder.
    /// </summary>
    public VoteDto? Vote { get; set; }

    /// <summary>
    /// The current status of the ballot after the mutation.
    /// </summary>
    public BallotStatus BallotStatusCode { get; set; }

    /// <summary>
    /// Authoritative row-id to position mapping after delete, reorder, or create when
    /// pre-existing votes were compacted. Omitted when positions are unchanged.
    /// </summary>
    public List<VotePositionDto>? VotePositions { get; set; }
}