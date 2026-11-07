namespace Backend.DTOs.Results;

/// <summary>
/// Details about a tie group. A resolved runoff stays here so its counts can be corrected.
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
    /// List of people involved in the tie.
    /// </summary>
    public List<TiePersonDto> People { get; set; } = new();

    /// <summary>
    /// Instructions for resolving the tie. Empty once the saved counts have decided the seats.
    /// </summary>
    public string Instructions { get; set; } = "Enter tie-break vote counts for these people";

    /// <summary>
    /// True while this group still needs a runoff. False when the counts have decided the seats,
    /// and when the tie never crossed a section boundary.
    /// </summary>
    public bool TieBreakRequired { get; set; }

    /// <summary>
    /// True when this group needed a runoff and the saved counts have decided it.
    /// </summary>
    public bool IsResolved { get; set; }
}

/// <summary>
/// Information about a person involved in a tie.
/// </summary>
public class TiePersonDto
{
    /// <summary>
    /// The unique identifier of the person.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// The full name of the person.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// The number of votes the person received.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// The tie-break vote count for this person.
    /// Null means no count has been entered; 0 is an explicit runoff result.
    /// </summary>
    public int? TieBreakCount { get; set; }
}

/// <summary>
/// Request to save tie-break vote counts.
/// </summary>
public class SaveTieCountsRequestDto
{
    /// <summary>
    /// List of tie-break counts for people.
    /// </summary>
    public List<TieCountDto> Counts { get; set; } = new();
}

/// <summary>
/// Tie-break count for a specific person.
/// </summary>
public class TieCountDto
{
    /// <summary>
    /// The unique identifier of the person.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// The tie-break vote count for this person.
    /// Null unsets a previous count. 0 is an explicit runoff result.
    /// Omit the person entirely to leave the stored count unchanged.
    /// </summary>
    public int? TieBreakCount { get; set; }
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


