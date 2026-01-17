using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Elections;
using TallyJ4.DTOs.Setup;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SetupController : ControllerBase
{
    private readonly ISetupService _setupService;
    private readonly ILogger<SetupController> _logger;

    public SetupController(ISetupService setupService, ILogger<SetupController> logger)
    {
        _setupService = setupService;
        _logger = logger;
    }

    [HttpPost("election/step1")]
    public async Task<ActionResult<ApiResponse<ElectionDto>>> CreateElectionStep1(ElectionStep1Dto step1Dto)
    {
        var election = await _setupService.CreateElectionStep1Async(step1Dto);

        return CreatedAtAction(
            nameof(GetSetupStatus),
            new { guid = election.ElectionGuid },
            ApiResponse<ElectionDto>.SuccessResponse(election, "Election created successfully (Step 1)"));
    }

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
