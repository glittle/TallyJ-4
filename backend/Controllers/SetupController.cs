using Backend.DTOs.Elections;
using Backend.DTOs.Setup;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing election setup operations in multiple steps.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SetupController : ControllerBase
{
    private readonly ISetupService _setupService;
    private readonly ILogger<SetupController> _logger;

    /// <summary>
    /// Initializes a new instance of the SetupController.
    /// </summary>
    /// <param name="setupService">The setup service for election configuration operations.</param>
    /// <param name="logger">The logger for recording operations.</param>
    public SetupController(ISetupService setupService, ILogger<SetupController> logger)
    {
        _setupService = setupService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new election with basic information (Step 1 of setup).
    /// </summary>
    /// <param name="step1Dto">The basic election setup information.</param>
    /// <returns>The created election information.</returns>
    [HttpPost("election/step1")]
    public async Task<ActionResult<ApiResponse<ElectionDto>>> CreateElectionStep1(ElectionStep1Dto step1Dto)
    {
        var election = await _setupService.CreateElectionStep1Async(step1Dto);

        return CreatedAtAction(
            nameof(GetSetupStatus),
            new { guid = election.ElectionGuid },
            ApiResponse<ElectionDto>.SuccessResponse(election, "Election created successfully (Step 1)"));
    }

    /// <summary>
    /// Configures additional election settings (Step 2 of setup).
    /// </summary>
    /// <param name="guid">The GUID of the election to configure.</param>
    /// <param name="step2Dto">The additional election configuration information.</param>
    /// <returns>The updated election information.</returns>
    [HttpPut("election/{guid}/step2")]
    public async Task<ActionResult<ApiResponse<ElectionDto>>> ConfigureElectionStep2(Guid guid, ElectionStep2Dto step2Dto)
    {
        if (guid != step2Dto.ElectionGuid)
        {
            return BadRequest(ApiResponse<ElectionDto>.ErrorResponse("Election GUID mismatch"));
        }

        var election = await _setupService.ConfigureElectionStep2Async(guid, step2Dto);

        if (election == null)
        {
            return NotFound(ApiResponse<ElectionDto>.ErrorResponse("Election not found"));
        }

        return Ok(ApiResponse<ElectionDto>.SuccessResponse(election, "Election configured successfully (Step 2)"));
    }

    /// <summary>
    /// Gets the current setup status of an election.
    /// </summary>
    /// <param name="guid">The GUID of the election to check.</param>
    /// <returns>The election setup status information.</returns>
    [HttpGet("election/{guid}/status")]
    public async Task<ActionResult<ApiResponse<ElectionSetupStatusDto>>> GetSetupStatus(Guid guid)
    {
        var status = await _setupService.GetSetupStatusAsync(guid);

        if (status == null)
        {
            return NotFound(ApiResponse<ElectionSetupStatusDto>.ErrorResponse("Election not found"));
        }

        return Ok(ApiResponse<ElectionSetupStatusDto>.SuccessResponse(status));
    }
}



