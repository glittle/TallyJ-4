using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Public;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

/// <summary>
/// Controller for public operations that don't require authentication.
/// Provides information about available elections and system status.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly IPublicService _publicService;
    private readonly ILogger<PublicController> _logger;

    /// <summary>
    /// Initializes a new instance of the PublicController.
    /// </summary>
    /// <param name="publicService">The public service for non-authenticated operations.</param>
    /// <param name="logger">The logger for recording operations.</param>
    public PublicController(IPublicService publicService, ILogger<PublicController> logger)
    {
        _publicService = publicService;
        _logger = logger;
    }

    /// <summary>
    /// Gets public home page data including system information.
    /// </summary>
    /// <returns>Public home page information.</returns>
    [HttpGet("home")]
    public async Task<ActionResult<ApiResponse<PublicHomeDto>>> GetPublicHome()
    {
        var homeData = await _publicService.GetPublicHomeDataAsync();
        return Ok(ApiResponse<PublicHomeDto>.SuccessResponse(homeData, "Welcome to TallyJ 4"));
    }

    /// <summary>
    /// Gets a list of all available elections that are open for public access.
    /// </summary>
    /// <returns>A list of available elections.</returns>
    [HttpGet("elections")]
    public async Task<ActionResult<ApiResponse<List<AvailableElectionDto>>>> GetAvailableElections()
    {
        var elections = await _publicService.GetAvailableElectionsAsync();
        return Ok(ApiResponse<List<AvailableElectionDto>>.SuccessResponse(
            elections,
            $"Found {elections.Count} available election(s)"));
    }

    /// <summary>
    /// Gets the current status of a specific election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to check.</param>
    /// <returns>The election status information.</returns>
    [HttpGet("elections/{electionGuid}/status")]
    public async Task<ActionResult<ApiResponse<ElectionStatusDto>>> GetElectionStatus(Guid electionGuid)
    {
        var status = await _publicService.GetElectionStatusAsync(electionGuid);

        if (status == null)
        {
            return NotFound(ApiResponse<ElectionStatusDto>.ErrorResponse(
                "Election not found",
                new List<string> { $"No election found with GUID: {electionGuid}" }));
        }

        return Ok(ApiResponse<ElectionStatusDto>.SuccessResponse(status));
    }

    /// <summary>
    /// Gets public display data for a specific election, including results formatted for full-screen presentation.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to display.</param>
    /// <returns>The public display data with election results.</returns>
    [HttpGet("elections/{electionGuid}/display")]
    public async Task<ActionResult<ApiResponse<PublicDisplayDto>>> GetPublicDisplay(Guid electionGuid)
    {
        var displayData = await _publicService.GetPublicDisplayDataAsync(electionGuid);

        if (displayData == null)
        {
            return NotFound(ApiResponse<PublicDisplayDto>.ErrorResponse(
                "Election not found or not available for public display",
                new List<string> { $"No public display available for election: {electionGuid}" }));
        }

        return Ok(ApiResponse<PublicDisplayDto>.SuccessResponse(displayData));
    }

    /// <summary>
    /// Performs a health check to verify the service is running properly.
    /// </summary>
    /// <returns>Health status information including timestamp and service details.</returns>
    [HttpGet("health")]
    public ActionResult<ApiResponse<object>> HealthCheck()
    {
        return Ok(ApiResponse<object>.SuccessResponse(
            new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "TallyJ 4 API"
            },
            "Service is running"));
    }
}
