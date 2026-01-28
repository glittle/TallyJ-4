using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Elections;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

/// <summary>
/// Controller for managing election operations including creation, retrieval, updates, and deletion.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ElectionsController : ControllerBase
{
    private readonly IElectionService _electionService;
    private readonly ILogger<ElectionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the ElectionsController.
    /// </summary>
    /// <param name="electionService">The election service for election operations.</param>
    /// <param name="logger">The logger for recording operations.</param>
    public ElectionsController(IElectionService electionService, ILogger<ElectionsController> logger)
    {
        _electionService = electionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of elections with optional status filtering.
    /// </summary>
    /// <param name="pageNumber">The page number (starting from 1).</param>
    /// <param name="pageSize">The number of items per page (1-100).</param>
    /// <param name="status">Optional status filter for elections.</param>
    /// <returns>A paginated response containing the elections.</returns>
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

    /// <summary>
    /// Gets a specific election by its GUID.
    /// </summary>
    /// <param name="guid">The GUID of the election.</param>
    /// <returns>The election information.</returns>
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

    /// <summary>
    /// Gets a summary of a specific election by its GUID.
    /// </summary>
    /// <param name="guid">The GUID of the election.</param>
    /// <returns>The election summary information.</returns>
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

    /// <summary>
    /// Creates a new election.
    /// </summary>
    /// <param name="createDto">The election creation data.</param>
    /// <returns>The created election information.</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ElectionDto>>> CreateElection(CreateElectionDto createDto)
    {
        var election = await _electionService.CreateElectionAsync(createDto);

        return CreatedAtAction(
            nameof(GetElection),
            new { guid = election.ElectionGuid },
            ApiResponse<ElectionDto>.SuccessResponse(election, "Election created successfully"));
    }

    /// <summary>
    /// Updates an existing election.
    /// </summary>
    /// <param name="guid">The GUID of the election to update.</param>
    /// <param name="updateDto">The updated election data.</param>
    /// <returns>The updated election information.</returns>
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

    /// <summary>
    /// Deletes an election by its GUID.
    /// </summary>
    /// <param name="guid">The GUID of the election to delete.</param>
    /// <returns>No content if successful, or not found if the election doesn't exist.</returns>
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
