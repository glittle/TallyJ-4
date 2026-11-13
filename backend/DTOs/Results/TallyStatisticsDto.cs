namespace Backend.DTOs.Results;

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
    /// Potential vote slots for a normal election: ballots × number to elect,
    /// including spoiled ballots and lines that were never typed. Single-name
    /// elections store the sum of vote counts instead.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Votes awarded to people. Normal elections count Ok lines on Ok ballots only.
    /// </summary>
    public int ValidVotes { get; set; }

    /// <summary>
    /// Spoiled lines on valid ballots, such as Unreadable. Lines on a spoiled
    /// ballot are not included here.
    /// </summary>
    public int InvalidVotes { get; set; }

    /// <summary>
    /// The number of registered voters.
    /// </summary>
    public int NumVoters { get; set; }

    /// <summary>
    /// The number of eligible people.
    /// </summary>
    public int NumEligiblePeople { get; set; }

    /// <summary>
    /// The number of positions to be elected.
    /// </summary>
    public int NumberToElect { get; set; }

    /// <summary>
    /// The number of extra positions beyond the required number.
    /// </summary>
    public int NumberExtra { get; set; }
}



