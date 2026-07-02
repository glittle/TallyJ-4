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
    /// The list of person results.
    /// </summary>
    public List<PersonResultDto> Results { get; set; } = new();

    /// <summary>
    /// Information about any ties in the election.
    /// </summary>
    public List<TieInfoDto> Ties { get; set; } = new();
}

/// <summary>
/// Data transfer object representing the result for a single person.
/// </summary>
public class PersonResultDto
{
    /// <summary>
    /// The unique identifier for the person.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// The full name of the person.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// The number of votes received by this person.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// The rank/position of this person in the results.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// The section this person belongs to (e.g., "E" for elected, "X" for extra).
    /// </summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>
    /// Whether this person is tied with others.
    /// </summary>
    public bool IsTied { get; set; }

    /// <summary>
    /// The tie break group number if this person is tied.
    /// </summary>
    public int? TieBreakGroup { get; set; }

    /// <summary>
    /// Whether tie breaking is required for this person.
    /// </summary>
    public bool TieBreakRequired { get; set; }

    /// <summary>
    /// Whether this person's vote count is close to the next person's count.
    /// </summary>
    public bool CloseToNext { get; set; }

    /// <summary>
    /// Whether this person's vote count is close to the previous person's count.
    /// </summary>
    public bool CloseToPrev { get; set; }
}



