using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TallyJ4.DTOs.Account;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

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

    /// <summary>
    /// Initializes a new instance of the AccountController.
    /// </summary>
    /// <param name="accountService">The account service for user operations.</param>
    /// <param name="logger">The logger for recording operations.</param>
    public AccountController(IAccountService accountService, ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the profile information for the authenticated user.
    /// </summary>
    /// <returns>The user's profile information.</returns>
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

    /// <summary>
    /// Updates the profile information for the authenticated user.
    /// </summary>
    /// <param name="updateDto">The updated profile information.</param>
    /// <returns>The updated user profile.</returns>
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

    /// <summary>
    /// Changes the password for the authenticated user.
    /// </summary>
    /// <param name="changePasswordDto">The password change information including current and new passwords.</param>
    /// <returns>A success response if the password was changed successfully.</returns>
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
