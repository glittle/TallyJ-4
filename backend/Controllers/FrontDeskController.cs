using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.FrontDesk;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/elections/{electionGuid}/frontdesk")]
[Authorize]
public class FrontDeskController : ControllerBase
{
    private readonly IFrontDeskService _frontDeskService;
    private readonly ILogger<FrontDeskController> _logger;

    public FrontDeskController(IFrontDeskService frontDeskService, ILogger<FrontDeskController> logger)
    {
        _frontDeskService = frontDeskService;
        _logger = logger;
    }

    [HttpGet("eligible-voters")]
    public async Task<ActionResult<ApiResponse<List<FrontDeskVoterDto>>>> GetEligibleVoters(Guid electionGuid)
    {
        try
        {
            var voters = await _frontDeskService.GetEligibleVotersAsync(electionGuid);
            return Ok(ApiResponse<List<FrontDeskVoterDto>>.SuccessResponse(voters));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting eligible voters for election {ElectionGuid}", electionGuid);
            return StatusCode(500, ApiResponse<List<FrontDeskVoterDto>>.ErrorResponse("Failed to retrieve eligible voters"));
        }
    }

    [HttpPost("checkin")]
    public async Task<ActionResult<ApiResponse<FrontDeskVoterDto>>> CheckInVoter(
        Guid electionGuid, 
        [FromBody] CheckInVoterDto checkInDto)
    {
        try
        {
            var voter = await _frontDeskService.CheckInVoterAsync(electionGuid, checkInDto);
            return Ok(ApiResponse<FrontDeskVoterDto>.SuccessResponse(voter, "Voter checked in successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<FrontDeskVoterDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking in voter {PersonGuid} for election {ElectionGuid}", 
                checkInDto.PersonGuid, electionGuid);
            return StatusCode(500, ApiResponse<FrontDeskVoterDto>.ErrorResponse("Failed to check in voter"));
        }
    }

    [HttpGet("rollcall")]
    public async Task<ActionResult<ApiResponse<RollCallDto>>> GetRollCall(Guid electionGuid)
    {
        try
        {
            var rollCall = await _frontDeskService.GetRollCallAsync(electionGuid);
            return Ok(ApiResponse<RollCallDto>.SuccessResponse(rollCall));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roll call for election {ElectionGuid}", electionGuid);
            return StatusCode(500, ApiResponse<RollCallDto>.ErrorResponse("Failed to retrieve roll call"));
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<FrontDeskStatsDto>>> GetStats(Guid electionGuid)
    {
        try
        {
            var stats = await _frontDeskService.GetStatsAsync(electionGuid);
            return Ok(ApiResponse<FrontDeskStatsDto>.SuccessResponse(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats for election {ElectionGuid}", electionGuid);
            return StatusCode(500, ApiResponse<FrontDeskStatsDto>.ErrorResponse("Failed to retrieve statistics"));
        }
    }
}
