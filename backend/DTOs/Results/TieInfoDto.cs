using Backend.Domain.Enumerations;

namespace Backend.DTOs.Results;

/// <summary>
/// Information about a tie situation in election results.
/// </summary>
public class TieInfoDto
{
    /// <summary>
    /// The tie-break group number for this tie.
    /// </summary>
    public int TieBreakGroup { get; set; }

    /// <summary>
    /// The vote count that all tied candidates received.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Whether a tie-break procedure is required to resolve this tie.
    /// </summary>
    public bool TieBreakRequired { get; set; }

    /// <summary>
    /// The election section or position where the tie occurred.
    /// </summary>
    public ResultSection SectionCode { get; set; }

    /// <summary>
    /// List of candidate names involved in the tie.
    /// </summary>
    public List<string> CandidateNames { get; set; } = new();
}



