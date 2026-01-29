namespace TallyJ4.DTOs.Results;

/// <summary>
/// Filtered report data with applied filters
/// </summary>
public class FilteredReportDto
{
    /// <summary>
    /// The filters that were applied to generate this report.
    /// </summary>
    public AdvancedFilterDto AppliedFilters { get; set; } = new();

    /// <summary>
    /// The total number of records before filtering.
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// The number of records after filtering has been applied.
    /// </summary>
    public int FilteredRecords { get; set; }

    /// <summary>
    /// Summary information about the election.
    /// </summary>
    public ElectionReportDto? Summary { get; set; }

    /// <summary>
    /// Filtered list of candidate results.
    /// </summary>
    public List<CandidateReportDto> Candidates { get; set; } = new();

    /// <summary>
    /// Filtered list of location results.
    /// </summary>
    public List<LocationReportDto> Locations { get; set; } = new();

    /// <summary>
    /// Filtered list of ballot results.
    /// </summary>
    public List<BallotReportDto> Ballots { get; set; } = new();

    /// <summary>
    /// Filtered list of voter information.
    /// </summary>
    public List<VoterReportDto> Voters { get; set; } = new();
}