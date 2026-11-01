using System.Security.Claims;
using Backend.DTOs.Account;
using Backend.DTOs.Owners;
using Backend.Helpers;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing user account operations including profile management and password changes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IOwnerReviewService _ownerReviewService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAccountService accountService,
        IOwnerReviewService ownerReviewService,
        ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _ownerReviewService = ownerReviewService;
        _logger = logger;
    }

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    [HttpGet("getMyProfile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<UserProfileDto>.ErrorResponse("User not authenticated"));
        }

        var profile = await _accountService.GetUserProfileAsync(userId);
        if (profile == null)
        {
            return NotFound(ApiResponse<UserProfileDto>.ErrorResponse("User profile not found"));
        }

        return Ok(ApiResponse<UserProfileDto>.SuccessResponse(profile));
    }

    [HttpPost("changeDisplayName")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> ChangeDisplayName(ChangeDisplayNameDto dto)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<UserProfileDto>.ErrorResponse("User not authenticated"));
        }

        try
        {
            var profile = await _accountService.ChangeDisplayNameAsync(userId, dto.DisplayName);
            if (profile == null)
            {
                return NotFound(ApiResponse<UserProfileDto>.ErrorResponse("User profile not found"));
            }

            return Ok(ApiResponse<UserProfileDto>.SuccessResponse(profile, "Display name updated"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<UserProfileDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("requestEmailChange")]
    public async Task<ActionResult<ApiResponse<object>>> RequestEmailChange(RequestEmailChangeDto dto)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
        }

        try
        {
            var clientIp = HttpContext.GetClientIpAddress();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            await _accountService.RequestEmailChangeAsync(userId, dto, clientIp, userAgent);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Verification sent to the new email address"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Email change request failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("changePassword")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordDto changePasswordDto)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
        }

        try
        {
            var success = await _accountService.ChangePasswordAsync(userId, changePasswordDto);
            if (!success)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Password changed successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Password change failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Restriction state for the signed-in account. Missing rows do not block sends.
    /// </summary>
    [HttpGet("owner-restriction")]
    public async Task<ActionResult<ApiResponse<OwnerRestrictionDto>>> GetOwnerRestriction()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<OwnerRestrictionDto>.ErrorResponse("User not authenticated"));
        }

        var restriction = await _ownerReviewService.GetForUserAsync(userId);
        return Ok(ApiResponse<OwnerRestrictionDto>.SuccessResponse(restriction));
    }

    /// <summary>
    /// Asks every super admin to review this account. Limited to one email per 24 hours.
    /// </summary>
    [HttpPost("owner-restriction/request-review")]
    public async Task<ActionResult<ApiResponse<OwnerRestrictionDto>>> RequestOwnerReview()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<OwnerRestrictionDto>.ErrorResponse("User not authenticated"));
        }

        var result = await _ownerReviewService.RequestReviewAsync(userId);
        if (!result.Success)
        {
            var status = result.ErrorKey == OwnerReviewService.ThrottledKey
                ? StatusCodes.Status429TooManyRequests
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, ApiResponse<OwnerRestrictionDto>.ErrorResponse(result.ErrorKey ?? "error"));
        }

        return Ok(ApiResponse<OwnerRestrictionDto>.SuccessResponse(result.Restriction!));
    }
}
