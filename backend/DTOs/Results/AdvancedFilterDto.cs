namespace TallyJ4.DTOs.Results;

/// <summary>
/// Advanced filtering options for reports
/// </summary>
public class AdvancedFilterDto
{
    public DateRangeFilterDto? DateRange { get; set; }
    public List<string>? Locations { get; set; }
    public List<string>? CandidateNames { get; set; }
    public NumericRangeFilterDto? VoteCountRange { get; set; }
    public NumericRangeFilterDto? TurnoutRange { get; set; }
    public List<string>? BallotStatuses { get; set; }
    public bool? OnlyElected { get; set; }
    public string? SortBy { get; set; } // "name", "votes", "turnout", etc.
    public string? SortOrder { get; set; } // "asc", "desc"
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}

public class DateRangeFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class NumericRangeFilterDto
{
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
}