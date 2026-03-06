using Backend.Domain.Enumerations;

namespace Backend.DTOs.Votes;

/// <summary>
/// Response DTO for vote create/update operations, containing the vote and the ballot's current status.
/// </summary>
public class VoteWithBallotStatusDto
{
    /// <summary>
    /// The vote that was created or updated.
    /// </summary>
    public VoteDto Vote { get; set; } = null!;

    /// <summary>
    /// The current status of the ballot containing this vote.
    /// </summary>
    public BallotStatus BallotStatusCode { get; set; }
}
