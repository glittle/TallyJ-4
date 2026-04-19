using Backend.Domain.Enumerations;

namespace Backend.DTOs.Results;

/// <summary>
/// Data transfer object containing election results formatted for presentation.
/// </summary>
public class PresentationDto
{
    /// <summary>
    /// Name of the election.
    /// </summary>
    public string ElectionName { get; set; } = string.Empty;

    /// <summary>
    /// Date when the election was held.
    /// </summary>
    public DateTimeOffset? ElectionDate { get; set; }

    /// <summary>
    /// Number of candidates to be elected.
    /// </summary>
    public int NumToElect { get; set; }

    /// <summary>
    /// Total number of ballots cast in the election.
    /// </summary>
    public int TotalBallots { get; set; }

    /// <summary>
    /// Total number of votes recorded across all ballots.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// List of candidates who have been elected.
    /// </summary>
    public List<PresentationCandidateDto> ElectedCandidates { get; set; } = new();

    /// <summary>
    /// List of additional candidates who received votes but were not elected.
    /// </summary>
    public List<PresentationCandidateDto> ExtraCandidates { get; set; } = new();

    /// <summary>
    /// Indicates whether there are any ties in the election results.
    /// </summary>
    public bool HasTies { get; set; }

    /// <summary>
    /// List of tie situations that need to be resolved.
    /// </summary>
    public List<PresentationTieDto> Ties { get; set; } = new();

    /// <summary>
    /// Current status of the election results ("Preliminary", "Final", "In Progress").
    /// </summary>
    public string Status { get; set; } = "Final";
}

/// <summary>
/// Data transfer object containing information about a candidate for presentation purposes.
/// </summary>
public class PresentationCandidateDto
{
    /// <summary>
    /// The ranking position of this candidate in the election results.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Full name of the candidate.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Number of votes received by this candidate.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Indicates whether this candidate is tied with others.
    /// </summary>
    public bool IsTied { get; set; }

    /// <summary>
    /// Indicates whether this candidate has been elected.
    /// </summary>
    public bool IsWinner { get; set; }
}

/// <summary>
/// Data transfer object containing information about a tie situation in election results.
/// </summary>
public class PresentationTieDto
{
    /// <summary>
    /// Group identifier for this tie situation.
    /// </summary>
    public int TieBreakGroup { get; set; }

    /// <summary>
    /// Section or category where the tie occurred.
    /// </summary>
    public ResultSection SectionCode { get; set; }

    /// <summary>
    /// List of candidate names involved in this tie.
    /// </summary>
    public List<string> CandidateNames { get; set; } = new();

    /// <summary>
    /// Indicates whether manual tie-breaking is required.
    /// </summary>
    public bool TieBreakRequired { get; set; }
}


