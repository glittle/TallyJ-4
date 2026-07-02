using Backend.Enumerations;

namespace Backend.DTOs.Public;

/// <summary>
/// Data transfer object for public display of election results.
/// Optimized for full-screen presentation on teller computers or public displays.
/// </summary>
public class PublicDisplayDto
{
    /// <summary>
    /// The unique identifier of the election.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the election.
    /// </summary>
    public string ElectionName { get; set; } = string.Empty;

    /// <summary>
    /// The date of the election.
    /// </summary>
    public DateTimeOffset? DateOfElection { get; set; }

    /// <summary>
    /// The name of the convenor/organizer.
    /// </summary>
    public string Convenor { get; set; } = string.Empty;

    /// <summary>
    /// The type of election (LSA, LSA1, LSA2, NSA, Con, Reg, Oth).
    /// </summary>
    public ElectionTypeCode? ElectionType { get; set; }

    /// <summary>
    /// The current stage of the election.
    /// </summary>
    public ElectionStage ElectionStage { get; set; }

    /// <summary>
    /// Number of positions to elect.
    /// </summary>
    public int NumberToElect { get; set; }

    /// <summary>
    /// Number of additional names to report.
    /// </summary>
    public int NumberExtra { get; set; }

    /// <summary>
    /// List of elected people.
    /// </summary>
    public List<PublicPersonDto> ElectedPeople { get; set; } = new();

    /// <summary>
    /// List of additional people (next highest).
    /// </summary>
    public List<PublicPersonDto> AdditionalPeople { get; set; } = new();

    /// <summary>
    /// Summary statistics about the election.
    /// </summary>
    public PublicDisplayStatsDto Statistics { get; set; } = new();

    /// <summary>
    /// Timestamp of when results were last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; }

    /// <summary>
    /// Whether the election results are finalized.
    /// </summary>
    public bool IsFinalized { get; set; }
}

/// <summary>
/// Data transfer object for a person in the public display.
/// </summary>
public class PublicPersonDto
{
    /// <summary>
    /// The rank/position of this person.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// The full name of the person.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// The number of votes received.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Whether this person is tied with others.
    /// </summary>
    public bool IsTied { get; set; }

    /// <summary>
    /// Whether tie breaking is required for this person.
    /// </summary>
    public bool TieBreakRequired { get; set; }
}

/// <summary>
/// Data transfer object for election statistics in the public display.
/// </summary>
public class PublicDisplayStatsDto
{
    /// <summary>
    /// Total number of ballots submitted.
    /// </summary>
    public int TotalBallots { get; set; }

    /// <summary>
    /// Number of valid ballots.
    /// </summary>
    public int ValidBallots { get; set; }

    /// <summary>
    /// Number of spoiled ballots.
    /// </summary>
    public int SpoiledBallots { get; set; }

    /// <summary>
    /// Total number of votes counted.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Total number of registered voters.
    /// </summary>
    public int RegisteredVoters { get; set; }

    /// <summary>
    /// Voter turnout percentage.
    /// </summary>
    public decimal TurnoutPercentage { get; set; }
}



