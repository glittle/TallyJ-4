using Backend.Enumerations;

namespace Backend.DTOs.Ballots;

/// <summary>
/// Data transfer object for updating ballot information.
/// </summary>
public class UpdateBallotDto
{
    /// <summary>
    /// The status of the ballot. Ignored while a ballot is in Review unless setting Review.
    /// </summary>
    public BallotStatus StatusCode { get; set; }

    /// <summary>
    /// When true, clears a manual Needs Review flag and re-evaluates status from votes.
    /// This is the only supported way to exit Review.
    /// </summary>
    public bool ClearNeedsReview { get; set; }

    /// <summary>
    /// The name of the first teller who processed the ballot.
    /// </summary>
    public string? Teller1 { get; set; }

    /// <summary>
    /// The name of the second teller who processed the ballot.
    /// </summary>
    public string? Teller2 { get; set; }
}



