namespace TallyJ4.DTOs.Results;

/// <summary>
/// Data transfer object representing statistical information about an election tally.
/// </summary>
public class TallyStatisticsDto
{
    /// <summary>
    /// The total number of ballots in the election.
    /// </summary>
    public int TotalBallots { get; set; }

    /// <summary>
    /// The number of ballots that have been received.
    /// </summary>
    public int BallotsReceived { get; set; }

    /// <summary>
    /// The number of spoiled/invalid ballots.
    /// </summary>
    public int SpoiledBallots { get; set; }

    /// <summary>
    /// The number of ballots that need manual review.
    /// </summary>
    public int BallotsNeedingReview { get; set; }

    /// <summary>
    /// The total number of votes cast.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// The number of valid votes.
    /// </summary>
    public int ValidVotes { get; set; }

    /// <summary>
    /// The number of invalid votes.
    /// </summary>
    public int InvalidVotes { get; set; }

    /// <summary>
    /// The number of registered voters.
    /// </summary>
    public int NumVoters { get; set; }

    /// <summary>
    /// The number of eligible candidates.
    /// </summary>
    public int NumEligibleCandidates { get; set; }

    /// <summary>
    /// The number of positions to be elected.
    /// </summary>
    public int NumberToElect { get; set; }

    /// <summary>
    /// The number of extra positions beyond the required number.
    /// </summary>
    public int NumberExtra { get; set; }
}
