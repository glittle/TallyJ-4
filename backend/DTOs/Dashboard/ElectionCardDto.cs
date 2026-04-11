namespace Backend.DTOs.Dashboard;

/// <summary>
/// Data transfer object representing an election card for dashboard display.
/// </summary>
public class ElectionCardDto
{
    /// <summary>
    /// The unique identifier for the election.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the election.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The date when the election will be held.
    /// </summary>
    public DateTimeOffset? DateOfElection { get; set; }

    /// <summary>
    /// The current tally status of the election.
    /// </summary>
    public string? TallyStatus { get; set; }

    /// <summary>
    /// The total number of registered voters.
    /// </summary>
    public int VoterCount { get; set; }

    /// <summary>
    /// The total number of ballots cast.
    /// </summary>
    public int BallotCount { get; set; }

    /// <summary>
    /// The total number of votes counted.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// The percentage of completion for the election tally.
    /// </summary>
    public double PercentComplete { get; set; }
}



