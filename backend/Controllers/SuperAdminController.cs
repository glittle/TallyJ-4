using Backend.DTOs.SuperAdmin;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for super admin functionality, providing dashboard and management capabilities.
/// </summary>
[ApiController]
[Route("api/superadmin")]
[Authorize]
public class SuperAdminController : ControllerBase
{
    private readonly ISuperAdminService _superAdminService;
    private readonly ILogger<SuperAdminController> _logger;

    /// <summary>
    /// Initializes a new instance of the SuperAdminController.
    /// </summary>
    /// <param name="superAdminService">The super admin service for business logic.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SuperAdminController(
        ISuperAdminService superAdminService,
        ILogger<SuperAdminController> logger)
    {
        _superAdminService = superAdminService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a summary of system-wide election statistics for the super admin dashboard.
    /// </summary>
    /// <returns>An ApiResponse containing the super admin summary data.</returns>
    [HttpGet("dashboard/summary")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<SuperAdminSummaryDto>>> GetSummary()
    {
        var summary = await _superAdminService.GetSummaryAsync();
        return Ok(ApiResponse<SuperAdminSummaryDto>.SuccessResponse(summary));
    }

    /// <summary>
    /// Gets a paginated list of elections for the super admin dashboard.
    /// </summary>
    /// <param name="filter">The filter criteria for querying elections.</param>
    /// <returns>An ApiResponse containing paginated election data.</returns>
    [HttpGet("dashboard/elections")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<SuperAdminElectionDto>>>> GetElections(
        [FromQuery] SuperAdminElectionFilterDto filter)
    {
        var result = await _superAdminService.GetElectionsAsync(filter);
        return Ok(ApiResponse<PaginatedResponse<SuperAdminElectionDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// Gets detailed information about a specific election for the super admin dashboard.
    /// </summary>
    /// <param name="guid">The unique identifier of the election.</param>
    /// <returns>An ApiResponse containing detailed election information, or NotFound if the election doesn't exist.</returns>
    [HttpGet("dashboard/elections/{guid:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<SuperAdminElectionDetailDto>>> GetElectionDetail(Guid guid)
    {
        var detail = await _superAdminService.GetElectionDetailAsync(guid);
        if (detail == null)
        {
            return NotFound(ApiResponse<SuperAdminElectionDetailDto>.ErrorResponse("Election not found"));
        }

        return Ok(ApiResponse<SuperAdminElectionDetailDto>.SuccessResponse(detail));
    }
}



