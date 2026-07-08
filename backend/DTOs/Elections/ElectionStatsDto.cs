namespace Backend.DTOs.Elections;

/// <summary>
/// Aggregate counts for an election (voters, ballots, locations).
/// </summary>
public class ElectionStatsDto
{
    /// <summary>
    /// The total number of registered voters.
    /// </summary>
    public int VoterCount { get; set; }

    /// <summary>
    /// The total number of ballots cast.
    /// </summary>
    public int BallotCount { get; set; }

    /// <summary>
    /// The number of voting locations.
    /// </summary>
    public int LocationCount { get; set; }
}