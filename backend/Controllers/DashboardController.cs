using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Dashboard;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetDashboardSummary()
    {
        var summary = await _dashboardService.GetDashboardSummaryAsync();
        return Ok(ApiResponse<DashboardSummaryDto>.SuccessResponse(summary));
    }

    [HttpGet("elections")]
    public async Task<ActionResult<ApiResponse<List<ElectionCardDto>>>> GetRecentElections(
        [FromQuery] int limit = 10)
    {
        if (limit < 1 || limit > 100)
        {
            return BadRequest(ApiResponse<List<ElectionCardDto>>.ErrorResponse(
                "Invalid limit parameter. Limit must be between 1 and 100."));
        }

        var elections = await _dashboardService.GetRecentElectionsAsync(limit);
        return Ok(ApiResponse<List<ElectionCardDto>>.SuccessResponse(elections));
    }
}
