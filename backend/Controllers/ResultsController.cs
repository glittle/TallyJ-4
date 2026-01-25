using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Results;
using TallyJ4.Services;

namespace TallyJ4.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ResultsController : ControllerBase
{
    private readonly ITallyService _tallyService;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly ILogger<ResultsController> _logger;

    public ResultsController(
        ITallyService tallyService,
        ISignalRNotificationService signalRNotificationService,
        ILogger<ResultsController> logger)
    {
        _tallyService = tallyService;
        _signalRNotificationService = signalRNotificationService;
        _logger = logger;
    }

    [HttpPost("election/{electionGuid:guid}/calculate")]
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

    [HttpGet("election/{electionGuid:guid}")]
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

    [HttpGet("election/{electionGuid:guid}/summary")]
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

    [HttpGet("election/{electionGuid:guid}/final")]
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

    [HttpPost("election/{electionGuid:guid}/monitor/refresh")]
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

    [HttpGet("election/{electionGuid:guid}/monitor")]
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

    [HttpGet("election/{electionGuid:guid}/ties/{tieBreakGroup:int}")]
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

    [HttpPost("election/{electionGuid:guid}/ties")]
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

    [HttpGet("election/{electionGuid:guid}/report")]
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

    [HttpGet("election/{electionGuid:guid}/report/{reportCode}")]
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

    [HttpGet("election/{electionGuid:guid}/presentation")]
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
}
