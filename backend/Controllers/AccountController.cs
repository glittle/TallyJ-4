using System.Security.Claims;
using Backend.DTOs.Account;
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
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAccountService accountService, ILogger<AccountController> logger)
    {
        _accountService = accountService;
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

    [HttpPut("updateProfile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile(UpdateUserProfileDto updateDto)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<UserProfileDto>.ErrorResponse("User not authenticated"));
        }

        try
        {
            var profile = await _accountService.UpdateUserProfileAsync(userId, updateDto);
            if (profile == null)
            {
                return NotFound(ApiResponse<UserProfileDto>.ErrorResponse("User profile not found"));
            }

            return Ok(ApiResponse<UserProfileDto>.SuccessResponse(profile, "Profile updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Profile update failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<UserProfileDto>.ErrorResponse(ex.Message));
        }
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
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
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

    /// <summary>
    /// Confirms a pending email change (authenticated code path or token while signed in).
    /// Anonymous token confirmation is also available on AuthController.
    /// </summary>
    [HttpPost("confirmEmailChange")]
    public async Task<ActionResult<ApiResponse<object>>> ConfirmEmailChange(ConfirmEmailChangeDto dto)
    {
        var userId = CurrentUserId;
        try
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            await _accountService.ConfirmEmailChangeAsync(userId, dto, clientIp, userAgent);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Email address updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
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
}
