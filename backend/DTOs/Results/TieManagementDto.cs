namespace Backend.DTOs.Results;

/// <summary>
/// Details about a tie situation that requires manual resolution.
/// </summary>
public class TieDetailsDto
{
    /// <summary>
    /// The tie-break group number for this tie.
    /// </summary>
    public int TieBreakGroup { get; set; }

    /// <summary>
    /// The election section or position where the tie occurred.
    /// </summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>
    /// List of candidates involved in the tie.
    /// </summary>
    public List<TieCandidateDto> Candidates { get; set; } = new();

    /// <summary>
    /// Instructions for resolving the tie.
    /// </summary>
    public string Instructions { get; set; } = "Enter tie-break vote counts for these candidates";
}

/// <summary>
/// Information about a candidate involved in a tie.
/// </summary>
public class TieCandidateDto
{
    /// <summary>
    /// The unique identifier of the person.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// The full name of the candidate.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// The number of votes the candidate received.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// The tie-break vote count for this candidate.
    /// </summary>
    public int? TieBreakCount { get; set; }
}

/// <summary>
/// Request to save tie-break vote counts.
/// </summary>
public class SaveTieCountsRequestDto
{
    /// <summary>
    /// List of tie-break counts for candidates.
    /// </summary>
    public List<TieCountDto> Counts { get; set; } = new();
}

/// <summary>
/// Tie-break count for a specific candidate.
/// </summary>
public class TieCountDto
{
    /// <summary>
    /// The unique identifier of the person.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// The tie-break vote count for this candidate.
    /// </summary>
    public int TieBreakCount { get; set; }
}

/// <summary>
/// Response from saving tie-break vote counts.
/// </summary>
public class SaveTieCountsResponseDto
{
    /// <summary>
    /// Whether the save operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// A message describing the result of the operation.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether a re-analysis of the election results was triggered.
    /// </summary>
    public bool ReAnalysisTriggered { get; set; }
}


