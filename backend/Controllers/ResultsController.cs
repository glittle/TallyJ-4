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
    private readonly ILogger<ResultsController> _logger;

    public ResultsController(ITallyService tallyService, ILogger<ResultsController> logger)
    {
        _tallyService = tallyService;
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
}
