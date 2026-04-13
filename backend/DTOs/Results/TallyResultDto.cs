using Backend.Domain.Enumerations;

namespace Backend.DTOs.Results;

/// <summary>
/// Data transfer object representing the complete results of an election tally.
/// </summary>
public class TallyResultDto
{
    /// <summary>
    /// The unique identifier for the election.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the election.
    /// </summary>
    public string ElectionName { get; set; } = string.Empty;

    /// <summary>
    /// The date and time when the tally was calculated.
    /// </summary>
    public DateTimeOffset CalculatedAt { get; set; }

    /// <summary>
    /// Statistical information about the tally.
    /// </summary>
    public TallyStatisticsDto Statistics { get; set; } = new();

    /// <summary>
    /// The list of candidate results.
    /// </summary>
    public List<CandidateResultDto> Results { get; set; } = new();

    /// <summary>
    /// Information about any ties in the election.
    /// </summary>
    public List<TieInfoDto> Ties { get; set; } = new();
}

/// <summary>
/// Data transfer object representing the result for a single candidate.
/// </summary>
public class CandidateResultDto
{
    /// <summary>
    /// The unique identifier for the person (candidate).
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// The full name of the candidate.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// The number of votes received by this candidate.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// The rank/position of this candidate in the results.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// The section this candidate belongs to (e.g., "E" for elected, "X" for extra).
    /// </summary>
    public ResultSection SectionCode { get; set; }

    /// <summary>
    /// Whether this candidate is tied with others.
    /// </summary>
    public bool IsTied { get; set; }

    /// <summary>
    /// The tie break group number if this candidate is tied.
    /// </summary>
    public int? TieBreakGroup { get; set; }

    /// <summary>
    /// Whether tie breaking is required for this candidate.
    /// </summary>
    public bool TieBreakRequired { get; set; }

    /// <summary>
    /// Whether this candidate's vote count is close to the next candidate's count.
    /// </summary>
    public bool CloseToNext { get; set; }

    /// <summary>
    /// Whether this candidate's vote count is close to the previous candidate's count.
    /// </summary>
    public bool CloseToPrev { get; set; }
}



