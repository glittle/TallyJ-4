using Backend.DTOs.Results;

namespace Backend.Services;

/// <summary>
/// Service interface for advanced reporting features
/// </summary>
public interface IAdvancedReportingService
{
    /// <summary>
    /// Generates chart data for visualization
    /// </summary>
    Task<ChartDataDto> GenerateChartDataAsync(Guid electionId, string chartType);

    /// <summary>
    /// Compares multiple elections
    /// </summary>
    Task<ElectionComparisonDto> CompareElectionsAsync(List<Guid> electionIds, List<string> metrics);

    /// <summary>
    /// Generates a filtered report based on advanced criteria
    /// </summary>
    Task<FilteredReportDto> GenerateFilteredReportAsync(Guid electionId, AdvancedFilterDto filters);

    /// <summary>
    /// Generates a custom report based on configuration
    /// </summary>
    Task<CustomReportDto> GenerateCustomReportAsync(CustomReportConfigDto config);

    /// <summary>
    /// Generates comprehensive statistical analysis
    /// </summary>
    Task<StatisticalAnalysisDto> GenerateStatisticalAnalysisAsync(Guid electionId);
}


