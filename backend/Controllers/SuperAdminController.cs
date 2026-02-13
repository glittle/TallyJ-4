using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TallyJ4.Authorization;
using TallyJ4.DTOs.SuperAdmin;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Controllers;

[ApiController]
[Route("api/superadmin")]
[Authorize]
public class SuperAdminController : ControllerBase
{
    private readonly ISuperAdminService _superAdminService;
    private readonly SuperAdminSettings _settings;
    private readonly ILogger<SuperAdminController> _logger;

    public SuperAdminController(
        ISuperAdminService superAdminService,
        IOptions<SuperAdminSettings> settings,
        ILogger<SuperAdminController> logger)
    {
        _superAdminService = superAdminService;
        _settings = settings.Value;
        _logger = logger;
    }

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

    [HttpGet("dashboard/summary")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<SuperAdminSummaryDto>>> GetSummary()
    {
        var summary = await _superAdminService.GetSummaryAsync();
        return Ok(ApiResponse<SuperAdminSummaryDto>.SuccessResponse(summary));
    }

    [HttpGet("dashboard/elections")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<SuperAdminElectionDto>>>> GetElections(
        [FromQuery] SuperAdminElectionFilterDto filter)
    {
        var result = await _superAdminService.GetElectionsAsync(filter);
        return Ok(ApiResponse<PaginatedResponse<SuperAdminElectionDto>>.SuccessResponse(result));
    }

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
