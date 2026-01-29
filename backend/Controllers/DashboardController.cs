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

    /// <summary>
    /// Gets a summary of dashboard information including active elections and key metrics.
    /// </summary>
    /// <returns>An API response containing the dashboard summary data.</returns>
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetDashboardSummary()
    {
        var summary = await _dashboardService.GetDashboardSummaryAsync();
        return Ok(ApiResponse<DashboardSummaryDto>.SuccessResponse(summary));
    }

    /// <summary>
    /// Gets a list of recent elections accessible to the current user.
    /// </summary>
    /// <param name="limit">The maximum number of elections to return (1-100).</param>
    /// <returns>An API response containing the list of recent elections.</returns>
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

    /// <summary>
    /// Gets dashboard summary information (legacy route for backward compatibility).
    /// </summary>
    /// <returns>An API response containing the dashboard summary data.</returns>
    [HttpGet("index")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetDashboardIndex()
    {
        return await GetDashboardSummary();
    }

    /// <summary>
    /// Gets a complete list of all elections accessible to the current user.
    /// </summary>
    /// <returns>An API response containing the list of all accessible elections.</returns>
    [HttpGet("election-list")]
    public async Task<ActionResult<ApiResponse<List<ElectionCardDto>>>> GetElectionList()
    {
        var elections = await _dashboardService.GetAllAccessibleElectionsAsync();
        return Ok(ApiResponse<List<ElectionCardDto>>.SuccessResponse(elections));
    }

    /// <summary>
    /// Gets static information about a specific election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to get information for.</param>
    /// <returns>An API response containing static election information.</returns>
    [HttpPost("more-info-static")]
    public async Task<ActionResult<ApiResponse<object>>> GetElectionStaticInfo([FromBody] Guid electionGuid)
    {
        var info = await _dashboardService.GetElectionStaticInfoAsync(electionGuid);
        return Ok(ApiResponse<object>.SuccessResponse(info));
    }

    /// <summary>
    /// Gets live statistics for a specific election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to get live statistics for.</param>
    /// <returns>An API response containing live election statistics.</returns>
    [HttpPost("more-info-live")]
    public async Task<ActionResult<ApiResponse<object>>> GetElectionLiveStats([FromBody] Guid electionGuid)
    {
        var stats = await _dashboardService.GetElectionLiveStatsAsync(electionGuid);
        return Ok(ApiResponse<object>.SuccessResponse(stats));
    }

    /// <summary>
    /// Refreshes and returns the complete list of elections accessible to the current user.
    /// </summary>
    /// <returns>An API response containing the refreshed list of all accessible elections.</returns>
    [HttpPost("reload-elections")]
    public async Task<ActionResult<ApiResponse<List<ElectionCardDto>>>> ReloadElections()
    {
        var elections = await _dashboardService.GetAllAccessibleElectionsAsync();
        return Ok(ApiResponse<List<ElectionCardDto>>.SuccessResponse(elections));
    }

    /// <summary>
    /// Updates the public listing status of an election for guest tellers.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to update.</param>
    /// <param name="isListed">Whether the election should be publicly listed.</param>
    /// <returns>An API response indicating success or failure of the update.</returns>
    [HttpPost("update-listing/{electionGuid:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateElectionListing(
        Guid electionGuid,
        [FromBody] bool isListed)
    {
        var result = await _electionService.UpdateElectionListingAsync(electionGuid, isListed);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }

    /// <summary>
    /// Imports an election from TallyJ V2 format (legacy migration feature).
    /// </summary>
    /// <param name="v2ElectionData">The V2 election data to import.</param>
    /// <returns>An API response indicating the result of the import operation.</returns>
    [HttpPost("load-v2-election")]
    public async Task<ActionResult<ApiResponse<object>>> LoadV2Election([FromBody] string v2ElectionData)
    {
        // This would be a complex migration operation
        // For now, return not implemented
        return StatusCode(501, ApiResponse<object>.ErrorResponse("V2 election import not yet implemented"));
    }

    /// <summary>
    /// Sets the physical location for a computer in the election system.
    /// </summary>
    /// <param name="request">The request containing computer code and location GUID.</param>
    /// <returns>An API response indicating success or failure of the location assignment.</returns>
    [HttpPost("choose-location")]
    public async Task<ActionResult<ApiResponse<bool>>> ChooseLocation(
        [FromBody] ChooseLocationRequest request)
    {
        var result = await _dashboardService.SetComputerLocationAsync(request.ComputerCode, request.LocationGuid);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }

    /// <summary>
    /// Assigns a guest teller to an election.
    /// </summary>
    /// <param name="request">The request containing election GUID and teller name.</param>
    /// <returns>An API response indicating success or failure of the teller assignment.</returns>
    [HttpPost("choose-teller")]
    public async Task<ActionResult<ApiResponse<bool>>> ChooseTeller(
        [FromBody] ChooseTellerRequest request)
    {
        var result = await _dashboardService.AssignGuestTellerAsync(request.ElectionGuid, request.TellerName);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }

    /// <summary>
    /// Removes a guest teller from an election.
    /// </summary>
    /// <param name="request">The request containing election GUID and teller name to remove.</param>
    /// <returns>An API response indicating success or failure of the teller removal.</returns>
    [HttpPost("delete-teller")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTeller(
        [FromBody] DeleteTellerRequest request)
    {
        var result = await _dashboardService.RemoveGuestTellerAsync(request.ElectionGuid, request.TellerName);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }
}

// Request DTOs for dashboard operations

/// <summary>
/// Request model for setting a computer's physical location.
/// </summary>
public class ChooseLocationRequest
{
    /// <summary>
    /// The computer code identifier.
    /// </summary>
    public string ComputerCode { get; set; } = string.Empty;

    /// <summary>
    /// The GUID of the location to assign to the computer.
    /// </summary>
    public Guid LocationGuid { get; set; }
}

/// <summary>
/// Request model for assigning a guest teller to an election.
/// </summary>
public class ChooseTellerRequest
{
    /// <summary>
    /// The GUID of the election to assign the teller to.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the teller to assign.
    /// </summary>
    public string TellerName { get; set; } = string.Empty;
}

/// <summary>
/// Request model for removing a guest teller from an election.
/// </summary>
public class DeleteTellerRequest
{
    /// <summary>
    /// The GUID of the election to remove the teller from.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the teller to remove.
    /// </summary>
    public string TellerName { get; set; } = string.Empty;
}
