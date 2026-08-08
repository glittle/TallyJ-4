using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Backend.DTOs.Auth;
using Backend.Services.Auth;
using Backend.Authorization;
using Backend;
using Backend.Configuration;
using Backend.Context;
using Backend.Identity;
using Backend.DTOs.Security;
using Backend.Helpers;
using Backend.Middleware;
using Backend.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Controllers;

public partial class AuthController
{
    [HttpPost("forgotPassword")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var (success, error) = await _passwordResetService.GenerateResetTokenAsync(request);

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(new { message = "Password reset email sent if account exists" });
    }

    /// <summary>
    /// Resets a user's password using a reset token.
    /// </summary>
    /// <param name="request">The reset password request containing the token and new password.</param>
    /// <returns>A success message if the password was reset, or an error if the request fails.</returns>
    [HttpPost("resetPassword")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var (success, error) = await _passwordResetService.ResetPasswordAsync(request);

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(new { message = "Password reset successful" });
    }

    /// <summary>
    /// Verifies a user's email address using a verification token.
    /// </summary>
    /// <param name="request">The verify email request containing the email and verification token.</param>
    /// <returns>A success message if the email was verified, or an error if the request fails.</returns>
    [HttpPost("verifyEmail")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest(new { error = "User not found" });
        }

        if (user.EmailConfirmed)
        {
            return BadRequest(new { error = "Email is already verified" });
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { error = errors });
        }

        return Ok(new { message = "Email verified successfully" });
    }

    /// <summary>
    /// Confirms a pending email change via email-link token and/or short code.
    /// Anonymous so confirmation links work without a session; when authenticated, claims user id is also passed.
    /// </summary>
    [HttpPost("confirmEmailChange")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmailChange([FromBody] Backend.DTOs.Account.ConfirmEmailChangeDto dto)
    {
        try
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            await _accountService.ConfirmEmailChangeAsync(userId, dto, clientIp, userAgent);
            return Ok(new { message = "Email address updated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Sets up two-factor authentication for the authenticated user.
    /// </summary>
    /// <returns>The 2FA setup information including QR code and secret key.</returns>

}
