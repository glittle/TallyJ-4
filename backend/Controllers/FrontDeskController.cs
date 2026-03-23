using Backend.DTOs.FrontDesk;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing front desk operations including voter check-in and roll call.
/// </summary>
[ApiController]
[Route("api/{electionGuid}/frontdesk")]
[Authorize]
public class FrontDeskController : ControllerBase
{
    private readonly IFrontDeskService _frontDeskService;
    private readonly ILogger<FrontDeskController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrontDeskController"/> class.
    /// </summary>
    /// <param name="frontDeskService">The front desk service.</param>
    /// <param name="logger">The logger.</param>
    public FrontDeskController(IFrontDeskService frontDeskService, ILogger<FrontDeskController> logger)
    {
        _frontDeskService = frontDeskService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of eligible voters for an election.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <returns>A list of eligible voters.</returns>
    [HttpGet("eligibleVoters")]
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

    /// <summary>
    /// Checks in a voter for an election.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <param name="checkInDto">The check-in data.</param>
    /// <returns>The updated voter information.</returns>
    [HttpPost("checkInVoter")]
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

    /// <summary>
    /// Gets the roll call information for an election.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <returns>The roll call information.</returns>
    [HttpGet("rollCall")]
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

    /// <summary>
    /// Gets the front desk statistics for an election.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <returns>The front desk statistics.</returns>
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

    /// <summary>
    /// Unregisters a voter (removes their check-in status).
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <param name="unregisterDto">The unregister data.</param>
    /// <returns>The updated voter information.</returns>
    [HttpPost("unregisterVoter")]
    public async Task<ActionResult<ApiResponse<FrontDeskVoterDto>>> UnregisterVoter(
        Guid electionGuid,
        [FromBody] UnregisterVoterDto unregisterDto)
    {
        try
        {
            var voter = await _frontDeskService.UnregisterVoterAsync(electionGuid, unregisterDto);
            return Ok(ApiResponse<FrontDeskVoterDto>.SuccessResponse(voter, "Voter unregistered successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<FrontDeskVoterDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering voter {PersonGuid} for election {ElectionGuid}",
                unregisterDto.PersonGuid, electionGuid);
            return StatusCode(500, ApiResponse<FrontDeskVoterDto>.ErrorResponse("Failed to unregister voter"));
        }
    }

    /// <summary>
    /// Updates the flags for a person.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <param name="updateFlagsDto">The flags update data.</param>
    /// <returns>The updated voter information.</returns>
    [HttpPost("updatePersonFlags")]
    public async Task<ActionResult<ApiResponse<FrontDeskVoterDto>>> UpdatePersonFlags(
        Guid electionGuid,
        [FromBody] UpdatePersonFlagsDto updateFlagsDto)
    {
        try
        {
            var voter = await _frontDeskService.UpdatePersonFlagsAsync(electionGuid, updateFlagsDto);
            return Ok(ApiResponse<FrontDeskVoterDto>.SuccessResponse(voter, "Person flags updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<FrontDeskVoterDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating person flags {PersonGuid} for election {ElectionGuid}",
                updateFlagsDto.PersonGuid, electionGuid);
            return StatusCode(500, ApiResponse<FrontDeskVoterDto>.ErrorResponse("Failed to update person flags"));
        }
    }
}



