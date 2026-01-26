using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Dashboard;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IElectionService _electionService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        IElectionService electionService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _electionService = electionService;
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

    // Legacy route compatibility - main dashboard landing page
    [HttpGet("index")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetDashboardIndex()
    {
        return await GetDashboardSummary();
    }

    // Get list of all elections user can access
    [HttpGet("election-list")]
    public async Task<ActionResult<ApiResponse<List<ElectionCardDto>>>> GetElectionList()
    {
        var elections = await _dashboardService.GetAllAccessibleElectionsAsync();
        return Ok(ApiResponse<List<ElectionCardDto>>.SuccessResponse(elections));
    }

    // Get static election information
    [HttpPost("more-info-static")]
    public async Task<ActionResult<ApiResponse<object>>> GetElectionStaticInfo([FromBody] Guid electionGuid)
    {
        var info = await _dashboardService.GetElectionStaticInfoAsync(electionGuid);
        return Ok(ApiResponse<object>.SuccessResponse(info));
    }

    // Get live election statistics
    [HttpPost("more-info-live")]
    public async Task<ActionResult<ApiResponse<object>>> GetElectionLiveStats([FromBody] Guid electionGuid)
    {
        var stats = await _dashboardService.GetElectionLiveStatsAsync(electionGuid);
        return Ok(ApiResponse<object>.SuccessResponse(stats));
    }

    // Refresh election list
    [HttpPost("reload-elections")]
    public async Task<ActionResult<ApiResponse<List<ElectionCardDto>>>> ReloadElections()
    {
        var elections = await _dashboardService.GetAllAccessibleElectionsAsync();
        return Ok(ApiResponse<List<ElectionCardDto>>.SuccessResponse(elections));
    }

    // Toggle public listing for guest tellers
    [HttpPost("update-listing/{electionGuid:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateElectionListing(
        Guid electionGuid,
        [FromBody] bool isListed)
    {
        var result = await _electionService.UpdateElectionListingAsync(electionGuid, isListed);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }

    // Import election from TallyJ V2 (legacy)
    [HttpPost("load-v2-election")]
    public async Task<ActionResult<ApiResponse<object>>> LoadV2Election([FromBody] string v2ElectionData)
    {
        // This would be a complex migration operation
        // For now, return not implemented
        return StatusCode(501, ApiResponse<object>.ErrorResponse("V2 election import not yet implemented"));
    }

    // Set computer's physical location
    [HttpPost("choose-location")]
    public async Task<ActionResult<ApiResponse<bool>>> ChooseLocation(
        [FromBody] ChooseLocationRequest request)
    {
        var result = await _dashboardService.SetComputerLocationAsync(request.ComputerCode, request.LocationGuid);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }

    // Assign guest teller
    [HttpPost("choose-teller")]
    public async Task<ActionResult<ApiResponse<bool>>> ChooseTeller(
        [FromBody] ChooseTellerRequest request)
    {
        var result = await _dashboardService.AssignGuestTellerAsync(request.ElectionGuid, request.TellerName);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }

    // Remove guest teller
    [HttpPost("delete-teller")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTeller(
        [FromBody] DeleteTellerRequest request)
    {
        var result = await _dashboardService.RemoveGuestTellerAsync(request.ElectionGuid, request.TellerName);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }
}

// Request DTOs for dashboard operations
public class ChooseLocationRequest
{
    public string ComputerCode { get; set; } = string.Empty;
    public Guid LocationGuid { get; set; }
}

public class ChooseTellerRequest
{
    public Guid ElectionGuid { get; set; }
    public string TellerName { get; set; } = string.Empty;
}

public class DeleteTellerRequest
{
    public Guid ElectionGuid { get; set; }
    public string TellerName { get; set; } = string.Empty;
}
