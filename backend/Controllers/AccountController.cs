using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TallyJ4.DTOs.Account;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

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

    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile(UpdateUserProfileDto updateDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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

    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordDto changePasswordDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
