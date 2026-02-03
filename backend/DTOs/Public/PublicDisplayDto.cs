namespace TallyJ4.DTOs.Public;

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
    public DateTime? DateOfElection { get; set; }

    /// <summary>
    /// The name of the convenor/organizer.
    /// </summary>
    public string Convenor { get; set; } = string.Empty;

    /// <summary>
    /// The type of election (e.g., "LSA Election", "Unit Convention").
    /// </summary>
    public string ElectionType { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the tally (e.g., "In Progress", "Finalized").
    /// </summary>
    public string TallyStatus { get; set; } = string.Empty;

    /// <summary>
    /// Number of positions to elect.
    /// </summary>
    public int NumberToElect { get; set; }

    /// <summary>
    /// Number of additional names to report.
    /// </summary>
    public int NumberExtra { get; set; }

    /// <summary>
    /// List of elected candidates.
    /// </summary>
    public List<PublicCandidateDto> ElectedCandidates { get; set; } = new();

    /// <summary>
    /// List of additional candidates (next highest).
    /// </summary>
    public List<PublicCandidateDto> AdditionalCandidates { get; set; } = new();

    /// <summary>
    /// Summary statistics about the election.
    /// </summary>
    public PublicDisplayStatsDto Statistics { get; set; } = new();

    /// <summary>
    /// Timestamp of when results were last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Whether the election results are finalized.
    /// </summary>
    public bool IsFinalized { get; set; }
}

/// <summary>
/// Data transfer object for a candidate in the public display.
/// </summary>
public class PublicCandidateDto
{
    /// <summary>
    /// The rank/position of this candidate.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// The full name of the candidate.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// The number of votes received.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Whether this candidate is tied with others.
    /// </summary>
    public bool IsTied { get; set; }

    /// <summary>
    /// Whether tie breaking is required for this candidate.
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
