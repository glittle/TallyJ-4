using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Elections;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ElectionsController : ControllerBase
{
    private readonly IElectionService _electionService;
    private readonly ILogger<ElectionsController> _logger;

    public ElectionsController(IElectionService electionService, ILogger<ElectionsController> logger)
    {
        _electionService = electionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ElectionSummaryDto>>> GetElections(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new { message = "Invalid pagination parameters. PageNumber must be >= 1, PageSize must be between 1 and 100." });
        }

        var result = await _electionService.GetElectionsAsync(pageNumber, pageSize, status);
        return Ok(result);
    }

    [HttpGet("{guid}")]
    [Authorize(Policy = "ElectionAccess")]
    public async Task<ActionResult<ApiResponse<ElectionDto>>> GetElection(Guid guid)
    {
        var election = await _electionService.GetElectionByGuidAsync(guid);

        if (election == null)
        {
            return NotFound(ApiResponse<ElectionDto>.ErrorResponse("Election not found"));
        }

        return Ok(ApiResponse<ElectionDto>.SuccessResponse(election));
    }

    [HttpGet("{guid}/summary")]
    [Authorize(Policy = "ElectionAccess")]
    public async Task<ActionResult<ApiResponse<ElectionDto>>> GetElectionSummary(Guid guid)
    {
        var election = await _electionService.GetElectionSummaryAsync(guid);

        if (election == null)
        {
            return NotFound(ApiResponse<ElectionDto>.ErrorResponse("Election not found"));
        }

        return Ok(ApiResponse<ElectionDto>.SuccessResponse(election));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ElectionDto>>> CreateElection(CreateElectionDto createDto)
    {
        var election = await _electionService.CreateElectionAsync(createDto);

        return CreatedAtAction(
            nameof(GetElection),
            new { guid = election.ElectionGuid },
            ApiResponse<ElectionDto>.SuccessResponse(election, "Election created successfully"));
    }

    [HttpPut("{guid}")]
    [Authorize(Policy = "ElectionAccess")]
    public async Task<ActionResult<ApiResponse<ElectionDto>>> UpdateElection(Guid guid, UpdateElectionDto updateDto)
    {
        var election = await _electionService.UpdateElectionAsync(guid, updateDto);

        if (election == null)
        {
            return NotFound(ApiResponse<ElectionDto>.ErrorResponse("Election not found"));
        }

        return Ok(ApiResponse<ElectionDto>.SuccessResponse(election, "Election updated successfully"));
    }

    [HttpDelete("{guid}")]
    [Authorize(Policy = "ElectionAccess")]
    public async Task<IActionResult> DeleteElection(Guid guid)
    {
        var success = await _electionService.DeleteElectionAsync(guid);

        if (!success)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Election not found"));
        }

        return NoContent();
    }
}
