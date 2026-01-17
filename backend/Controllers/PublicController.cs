using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Public;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly IPublicService _publicService;
    private readonly ILogger<PublicController> _logger;

    public PublicController(IPublicService publicService, ILogger<PublicController> logger)
    {
        _publicService = publicService;
        _logger = logger;
    }

    [HttpGet("home")]
    public async Task<ActionResult<ApiResponse<PublicHomeDto>>> GetPublicHome()
    {
        var homeData = await _publicService.GetPublicHomeDataAsync();
        return Ok(ApiResponse<PublicHomeDto>.SuccessResponse(homeData, "Welcome to TallyJ 4"));
    }

    [HttpGet("elections")]
    public async Task<ActionResult<ApiResponse<List<AvailableElectionDto>>>> GetAvailableElections()
    {
        var elections = await _publicService.GetAvailableElectionsAsync();
        return Ok(ApiResponse<List<AvailableElectionDto>>.SuccessResponse(
            elections, 
            $"Found {elections.Count} available election(s)"));
    }

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

    [HttpGet("health")]
    public ActionResult<ApiResponse<object>> HealthCheck()
    {
        return Ok(ApiResponse<object>.SuccessResponse(
            new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                service = "TallyJ 4 API"
            },
            "Service is running"));
    }
}
