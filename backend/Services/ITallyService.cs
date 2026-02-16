using Backend.DTOs.Results;

namespace Backend.Services;

/// <summary>
/// Service interface for election tally operations including calculation, results retrieval, and reporting.
/// Provides comprehensive functionality for managing election results and tie-breaking processes.
/// </summary>
public interface ITallyService
{
    /// <summary>
    /// Calculates the results for a normal election using the configured tally method.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to calculate.</param>
    /// <returns>The calculated tally results.</returns>
    Task<TallyResultDto> CalculateNormalElectionAsync(Guid electionGuid);

    /// <summary>
    /// Calculates the results for a single-name election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to calculate.</param>
    /// <returns>The calculated tally results.</returns>
    Task<TallyResultDto> CalculateSingleNameElectionAsync(Guid electionGuid);

    /// <summary>
    /// Retrieves the current tally results for an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>The tally results.</returns>
    Task<TallyResultDto> GetTallyResultsAsync(Guid electionGuid);

    /// <summary>
    /// Retrieves statistical information about the tally process.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>The tally statistics.</returns>
    Task<TallyStatisticsDto> GetTallyStatisticsAsync(Guid electionGuid);

    /// <summary>
    /// Retrieves monitoring information for the tally process.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>The monitor information.</returns>
    Task<MonitorInfoDto> GetMonitorInfoAsync(Guid electionGuid);

    /// <summary>
    /// Refreshes the contact information for a computer in the tally system.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="computerCode">The computer code to refresh.</param>
    Task RefreshComputerContactAsync(Guid electionGuid, string computerCode);

    /// <summary>
    /// Retrieves tie-breaking information for a specific tie-break group.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="tieBreakGroup">The tie-break group number.</param>
    /// <returns>The tie details.</returns>
    Task<TieDetailsDto> GetTiesAsync(Guid electionGuid, int tieBreakGroup);

    /// <summary>
    /// Saves the tie-breaking vote counts for an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="request">The tie counts request data.</param>
    /// <returns>The response containing the save results.</returns>
    Task<SaveTieCountsResponseDto> SaveTieCountsAsync(Guid electionGuid, SaveTieCountsRequestDto request);

    /// <summary>
    /// Generates a comprehensive election report.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>The election report data.</returns>
    Task<ElectionReportDto> GetElectionReportAsync(Guid electionGuid);

    /// <summary>
    /// Retrieves specific report data by report code.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="reportCode">The code identifying the specific report.</param>
    /// <returns>The report data response.</returns>
    Task<ReportDataResponseDto> GetReportDataAsync(Guid electionGuid, string reportCode);

    /// <summary>
    /// Retrieves detailed statistics for the election tally.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>The detailed statistics.</returns>
    Task<DetailedStatisticsDto> GetDetailedStatisticsAsync(Guid electionGuid);

    /// <summary>
    /// Retrieves presentation-ready data for displaying election results.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>The presentation data.</returns>
    Task<PresentationDto> GetPresentationDataAsync(Guid electionGuid);
}



