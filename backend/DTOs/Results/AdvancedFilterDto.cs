namespace Backend.DTOs.Results;

/// <summary>
/// Advanced filtering options for reports
/// </summary>
public class AdvancedFilterDto
{
    /// <summary>
    /// Date range filter for election dates.
    /// </summary>
    public DateRangeFilterDto? DateRange { get; set; }

    /// <summary>
    /// List of location names to filter by.
    /// </summary>
    public List<string>? Locations { get; set; }

    /// <summary>
    /// List of candidate names to filter by.
    /// </summary>
    public List<string>? CandidateNames { get; set; }

    /// <summary>
    /// Vote count range filter.
    /// </summary>
    public NumericRangeFilterDto? VoteCountRange { get; set; }

    /// <summary>
    /// Turnout percentage range filter.
    /// </summary>
    public NumericRangeFilterDto? TurnoutRange { get; set; }

    /// <summary>
    /// List of ballot statuses to filter by.
    /// </summary>
    public List<string>? BallotStatuses { get; set; }

    /// <summary>
    /// Filter to show only elected candidates.
    /// </summary>
    public bool? OnlyElected { get; set; }

    /// <summary>
    /// Field to sort by (name, votes, turnout, etc.).
    /// </summary>
    public string? SortBy { get; set; } // "name", "votes", "turnout", etc.

    /// <summary>
    /// Sort order (asc, desc).
    /// </summary>
    public string? SortOrder { get; set; } // "asc", "desc"

    /// <summary>
    /// Page number for pagination (1-based).
    /// </summary>
    public int? PageNumber { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int? PageSize { get; set; }
}

/// <summary>
/// Date range filter for filtering by date ranges.
/// </summary>
public class DateRangeFilterDto
{
    /// <summary>
    /// The start date of the range (inclusive).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// The end date of the range (inclusive).
    /// </summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Numeric range filter for filtering by minimum and maximum values.
/// </summary>
public class NumericRangeFilterDto
{
    /// <summary>
    /// The minimum value of the range (inclusive).
    /// </summary>
    public decimal? Min { get; set; }

    /// <summary>
    /// The maximum value of the range (inclusive).
    /// </summary>
    public decimal? Max { get; set; }
}


