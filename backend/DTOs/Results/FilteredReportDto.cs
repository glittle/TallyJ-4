namespace TallyJ4.DTOs.Results;

/// <summary>
/// Filtered report data with applied filters
/// </summary>
public class FilteredReportDto
{
    public AdvancedFilterDto AppliedFilters { get; set; } = new();
    public int TotalRecords { get; set; }
    public int FilteredRecords { get; set; }
    public ElectionReportDto? Summary { get; set; }
    public List<CandidateReportDto> Candidates { get; set; } = new();
    public List<LocationReportDto> Locations { get; set; } = new();
    public List<BallotReportDto> Ballots { get; set; } = new();
    public List<VoterReportDto> Voters { get; set; } = new();
}