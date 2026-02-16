using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Backend.Authorization;
using Backend.DTOs.SuperAdmin;
using Backend.Models;
using Backend.Services;

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
    private readonly SuperAdminSettings _settings;
    private readonly ILogger<SuperAdminController> _logger;

    /// <summary>
    /// Initializes a new instance of the SuperAdminController.
    /// </summary>
    /// <param name="superAdminService">The super admin service for business logic.</param>
    /// <param name="settings">The super admin configuration settings.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SuperAdminController(
        ISuperAdminService superAdminService,
        IOptions<SuperAdminSettings> settings,
        ILogger<SuperAdminController> logger)
    {
        _superAdminService = superAdminService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Checks if the current authenticated user is a super admin.
    /// </summary>
    /// <returns>An ApiResponse containing whether the user is a super admin.</returns>
    [HttpGet("check")]
    public ActionResult<ApiResponse<SuperAdminCheckDto>> Check()
    {
        var email = User.FindFirst("email")?.Value
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        var isSuperAdmin = !string.IsNullOrEmpty(email)
            && _settings.Emails.Any(e => string.Equals(e, email, StringComparison.OrdinalIgnoreCase));

        return Ok(ApiResponse<SuperAdminCheckDto>.SuccessResponse(new SuperAdminCheckDto
        {
            IsSuperAdmin = isSuperAdmin
        }));
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



