using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.DTOs.Results;
using Backend.Services;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing election results and tally operations.
/// Provides endpoints for calculating tallies, retrieving results, monitoring elections, and managing tie-breaking.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ResultsController : ControllerBase
{
    private readonly ITallyService _tallyService;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly ILogger<ResultsController> _logger;

    /// <summary>
    /// Initializes a new instance of the ResultsController.
    /// </summary>
    /// <param name="tallyService">The tally service for election result calculations.</param>
    /// <param name="signalRNotificationService">The SignalR service for real-time notifications.</param>
    /// <param name="logger">The logger for recording operations.</param>
    public ResultsController(
        ITallyService tallyService,
        ISignalRNotificationService signalRNotificationService,
        ILogger<ResultsController> logger)
    {
        _tallyService = tallyService;
        _signalRNotificationService = signalRNotificationService;
        _logger = logger;
    }

    /// <summary>
    /// Calculates the tally results for a specific election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to calculate results for.</param>
    /// <param name="electionType">The type of election calculation ("normal" or "singlename").</param>
    /// <returns>The calculated tally results.</returns>
    [HttpPost("/{electionGuid:guid}/calculateTally")]
    public async Task<ActionResult<TallyResultDto>> CalculateTally(
        Guid electionGuid,
        [FromQuery] string? electionType = "normal")
    {
        try
        {
            _logger.LogInformation("Starting tally calculation for election {ElectionGuid}, type: {ElectionType}",
                electionGuid, electionType);

            TallyResultDto result;

            if (electionType?.ToLower() == "singlename")
            {
                result = await _tallyService.CalculateSingleNameElectionAsync(electionGuid);
            }
            else
            {
                result = await _tallyService.CalculateNormalElectionAsync(electionGuid);
            }

            _logger.LogInformation("Tally calculation completed for election {ElectionGuid}", electionGuid);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Election {ElectionGuid} not found", electionGuid);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating tally for election {ElectionGuid}", electionGuid);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the tally results for a specific election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to get results for.</param>
    /// <returns>The tally results for the specified election.</returns>
    [HttpGet("{electionGuid:guid}/results")]
    public async Task<ActionResult<TallyResultDto>> GetResults(Guid electionGuid)
    {
        try
        {
            var result = await _tallyService.GetTallyResultsAsync(electionGuid);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Election {ElectionGuid} not found", electionGuid);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving results for election {ElectionGuid}", electionGuid);
            throw;
        }
    }

    /// <summary>
    /// Retrieves summary statistics for a specific election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to get statistics for.</param>
    /// <returns>The summary statistics for the specified election.</returns>
    [HttpGet("{electionGuid:guid}/summary")]
    public async Task<ActionResult<TallyStatisticsDto>> GetSummary(Guid electionGuid)
    {
        try
        {
            var statistics = await _tallyService.GetTallyStatisticsAsync(electionGuid);
            return Ok(statistics);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Election {ElectionGuid} not found", electionGuid);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving summary for election {ElectionGuid}", electionGuid);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the final election results (only elected and extra positions).
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to get final results for.</param>
    /// <returns>The final election results.</returns>
    [HttpGet("{electionGuid:guid}/final")]
    public async Task<ActionResult<TallyResultDto>> GetFinalResults(Guid electionGuid)
    {
        try
        {
            var result = await _tallyService.GetTallyResultsAsync(electionGuid);

            var finalResults = new TallyResultDto
            {
                ElectionGuid = result.ElectionGuid,
                ElectionName = result.ElectionName,
                CalculatedAt = result.CalculatedAt,
                Statistics = result.Statistics,
                Results = result.Results
                    .Where(r => r.Section == "E" || r.Section == "X")
                    .ToList(),
                Ties = result.Ties
                    .Where(t => t.Section == "E" || t.Section == "X")
                    .ToList()
            };

            return Ok(finalResults);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Election {ElectionGuid} not found", electionGuid);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving final results for election {ElectionGuid}", electionGuid);
            throw;
        }
    }

    /// <summary>
    /// Refreshes the monitor information and updates computer contact time.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election being monitored.</param>
    /// <param name="computerCode">The computer code identifier (defaults to "Unknown").</param>
    /// <returns>The updated monitor information.</returns>
    [HttpPost("{electionGuid:guid}/refreshMonitor")]
    public async Task<ActionResult<MonitorInfoDto>> RefreshMonitor(Guid electionGuid, [FromQuery] string computerCode = "Unknown")
    {
        try
        {
            // Refresh the computer's last contact time
            await _tallyService.RefreshComputerContactAsync(electionGuid, computerCode);

            // Get updated monitoring information
            var monitorInfo = await _tallyService.GetMonitorInfoAsync(electionGuid);

            // Send real-time update via SignalR
            await _signalRNotificationService.SendMonitorUpdateAsync(monitorInfo);

            return Ok(monitorInfo);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Election {ElectionGuid} not found", electionGuid);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing monitor for election {ElectionGuid}", electionGuid);
            throw;
        }
    }

    /// <summary>
    /// Retrieves monitoring information for a specific election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to get monitor info for.</param>
    /// <returns>The monitoring information for the specified election.</returns>
    [HttpGet("{electionGuid:guid}/monitor")]
    public async Task<ActionResult<MonitorInfoDto>> GetMonitorInfo(Guid electionGuid)
    {
        try
        {
            var monitorInfo = await _tallyService.GetMonitorInfoAsync(electionGuid);
            return Ok(monitorInfo);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Election {ElectionGuid} not found", electionGuid);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monitor info for election {ElectionGuid}", electionGuid);
            throw;
        }
    }

    /// <summary>
    /// Retrieves tie-breaking details for a specific tie break group in an election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="tieBreakGroup">The tie break group number.</param>
    /// <returns>The tie details for the specified group.</returns>
    [HttpGet("{electionGuid:guid}/{tieBreakGroup:int}/ties")]
    public async Task<ActionResult<TieDetailsDto>> GetTies(Guid electionGuid, int tieBreakGroup)
    {
        try
        {
            var tieDetails = await _tallyService.GetTiesAsync(electionGuid, tieBreakGroup);
            return Ok(tieDetails);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Tie break group {TieBreakGroup} not found in election {ElectionGuid}: {Message}",
                tieBreakGroup, electionGuid, ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ties for election {ElectionGuid}, group {TieBreakGroup}",
                electionGuid, tieBreakGroup);
            throw;
        }
    }

    /// <summary>
    /// Saves tie-breaking vote counts for an election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="request">The tie counts request data.</param>
    /// <returns>The response indicating the result of saving tie counts.</returns>
    [HttpPost("{electionGuid:guid}/saveTies")]
    public async Task<ActionResult<SaveTieCountsResponseDto>> SaveTieCounts(Guid electionGuid, [FromBody] SaveTieCountsRequestDto request)
    {
        try
        {
            var result = await _tallyService.SaveTieCountsAsync(electionGuid, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Election {ElectionGuid} not found", electionGuid);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tie counts for election {ElectionGuid}", electionGuid);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the complete election report for a specific election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to get the report for.</param>
    /// <returns>The complete election report.</returns>
    [HttpGet("{electionGuid:guid}/completeReport")]
    public async Task<ActionResult<ElectionReportDto>> GetElectionReport(Guid electionGuid)
    {
        try
        {
            var report = await _tallyService.GetElectionReportAsync(electionGuid);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Election {ElectionGuid} not found", electionGuid);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving election report for {ElectionGuid}", electionGuid);
            throw;
        }
    }

    /// <summary>
    /// Retrieves specific report data for an election by report code.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="reportCode">The code identifying the specific report.</param>
    /// <returns>The report data for the specified report code.</returns>
    [HttpGet("{electionGuid:guid}/{reportCode}/getReportData")]
    public async Task<ActionResult<ReportDataResponseDto>> GetReportData(Guid electionGuid, string reportCode)
    {
        try
        {
            var reportData = await _tallyService.GetReportDataAsync(electionGuid, reportCode);
            return Ok(reportData);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Report {ReportCode} not found for election {ElectionGuid}: {Message}",
                reportCode, electionGuid, ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report data for election {ElectionGuid}, code {ReportCode}",
                electionGuid, reportCode);
            throw;
        }
    }

    /// <summary>
    /// Retrieves presentation data for displaying election results.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to get presentation data for.</param>
    /// <returns>The presentation data for the specified election.</returns>
    [HttpGet("{electionGuid:guid}/presentationData")]
    public async Task<ActionResult<PresentationDto>> GetPresentationData(Guid electionGuid)
    {
        try
        {
            var presentationData = await _tallyService.GetPresentationDataAsync(electionGuid);
            return Ok(presentationData);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Election {ElectionGuid} not found", electionGuid);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving presentation data for election {ElectionGuid}", electionGuid);
            throw;
        }
    }

    /// <summary>
    /// Retrieves detailed statistics for a specific election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to get detailed statistics for.</param>
    /// <returns>The detailed statistics for the specified election.</returns>
    [HttpGet("{electionGuid:guid}/detailedStatistics")]
    public async Task<ActionResult<DetailedStatisticsDto>> GetDetailedStatistics(Guid electionGuid)
    {
        try
        {
            var statistics = await _tallyService.GetDetailedStatisticsAsync(electionGuid);
            return Ok(statistics);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Election {ElectionGuid} not found", electionGuid);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving detailed statistics for election {ElectionGuid}", electionGuid);
            throw;
        }
    }
}



