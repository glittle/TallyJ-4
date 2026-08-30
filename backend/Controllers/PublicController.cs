using Backend.DTOs.Public;
using Backend.Models;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for anonymous public discovery (guest teller join list), system health,
/// and the Twilio SMS status callback (v3 Public/SmsStatus).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PublicController(
    IPublicService publicService,
    ITwilioSmsStatusService twilioSmsStatusService) : ControllerBase
{
    private readonly IPublicService _publicService = publicService;
    private readonly ITwilioSmsStatusService _twilioSmsStatusService = twilioSmsStatusService;

    /// <summary>
    /// Gets public home page data including system information.
    /// </summary>
    /// <returns>Public home page information.</returns>
    [HttpGet("home")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PublicHomeDto>>> GetPublicHome()
    {
        var homeData = await _publicService.GetPublicHomeDataAsync();
        return Ok(ApiResponse<PublicHomeDto>.SuccessResponse(homeData, "Welcome to TallyJ 4"));
    }

    /// <summary>
    /// Gets elections currently open for guest teller join.
    /// </summary>
    /// <returns>A list of available elections.</returns>
    [HttpGet("elections")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<AvailableElectionDto>>>> GetAvailableElections()
    {
        var elections = await _publicService.GetAvailableElectionsAsync();
        return Ok(ApiResponse<List<AvailableElectionDto>>.SuccessResponse(
            elections,
            $"Found {elections.Count} available election(s)"));
    }

    /// <summary>
    /// Health check endpoint to verify that the API is running and responsive.
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
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

    /// <summary>
    /// Twilio message status callback (v3 <c>Public/SmsStatus</c>). Updates SmsLog when a
    /// row exists for the SID and auto-learns <c>OnlineVoter.SmsStatus</c> on selected
    /// terminal failures. Always returns 204 so the callback does not leak whether a
    /// voter row exists.
    /// </summary>
    [HttpPost("smsStatus")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> SmsStatus([FromForm] TwilioSmsStatusCallbackDto dto)
    {
        await _twilioSmsStatusService.ProcessCallbackAsync(
            dto.Sid,
            dto.Status,
            dto.To,
            dto.ErrorCode);
        return NoContent();
    }
}
