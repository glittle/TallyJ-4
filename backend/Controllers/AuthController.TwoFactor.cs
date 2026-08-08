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
    [HttpPost("setup2fa")]
    public async Task<IActionResult> Setup2FA()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var (success, error, response) = await _twoFactorService.SetupAsync(userId);

        if (!success)
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.TwoFactorSetup,
                UserId = userId,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"2FA setup failed: {error}",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Warning
            });
            return BadRequest(new { error });
        }

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.TwoFactorSetup,
            UserId = userId,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = "2FA setup initiated successfully",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        return Ok(response);
    }

    /// <summary>
    /// Enables two-factor authentication for the authenticated user.
    /// </summary>
    /// <param name="request">The enable 2FA request containing the verification code.</param>
    /// <returns>A success message if 2FA was enabled, or an error if the request fails.</returns>
    [Authorize]
    [HttpPost("enable2fa")]
    public async Task<IActionResult> Enable2FA([FromBody] Enable2FARequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var (success, error) = await _twoFactorService.EnableAsync(userId, request);

        if (!success)
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.TwoFactorEnabled,
                UserId = userId,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"2FA enable failed: {error}",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Warning
            });
            return BadRequest(new { error });
        }

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.TwoFactorEnabled,
            UserId = userId,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = "2FA enabled successfully",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        return Ok(new { message = "Two-factor authentication enabled" });
    }

    /// <summary>
    /// Disables two-factor authentication for the authenticated user.
    /// </summary>
    /// <param name="request">The disable 2FA request containing the verification code.</param>
    /// <returns>A success message if 2FA was disabled, or an error if the request fails.</returns>
    [Authorize]
    [HttpPost("disable2fa")]
    public async Task<IActionResult> Disable2FA([FromBody] Disable2FARequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var (success, error) = await _twoFactorService.DisableAsync(userId, request);

        if (!success)
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.TwoFactorDisabled,
                UserId = userId,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"2FA disable failed: {error}",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Warning
            });
            return BadRequest(new { error });
        }

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.TwoFactorDisabled,
            UserId = userId,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = "2FA disabled successfully",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        return Ok(new { message = "Two-factor authentication disabled" });
    }

    /// <summary>
    /// Gets the two-factor authentication status for the authenticated user.
    /// </summary>
    /// <returns>The 2FA status including whether it is enabled and the method used.</returns>
    [Authorize]
    [HttpGet("2fa/status")]
    public async Task<IActionResult> Get2FAStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var (success, error, isEnabled, method) = await _twoFactorService.GetStatusAsync(userId);
        if (!success)
        {
            return NotFound(new { error });
        }

        return Ok(new
        {
            isEnabled,
            method
        });
    }

    /// <summary>
    /// Verifies a two-factor authentication code for login.
    /// </summary>
    /// <param name="request">The verify 2FA request containing email, password, and verification code.</param>
    /// <returns>The authentication response with tokens if successful, or an error if verification fails.</returns>
    [HttpPost("verify2fa")]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FARequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var (success, error, response) = await _localAuthService.LoginAsync(new LoginRequest
        {
            Email = request.Email,
            Password = request.Password,
            TwoFactorCode = request.Code
        });

        if (!success)
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.TwoFactorVerificationFailure,
                Email = request.Email,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"2FA verification failed: {error}",
                IsSuspicious = true,
                Severity = SecurityEventSeverity.Warning
            });

            return BadRequest(new { error });
        }

        // Get user ID for logging
        var user = await _userManager.FindByEmailAsync(request.Email);
        var userId = user?.Id;

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.TwoFactorVerificationSuccess,
            UserId = userId,
            Email = request.Email,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = "2FA verification successful",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        // Set secure cookies with authentication data
        SecureCookieMiddleware.SetAuthCookies(
            HttpContext,
            response!.Token ?? "",
            response.RefreshToken ?? "",
            response.Email,
            response.Name,
            response.AuthMethod ?? "Local",
            HttpContext.Request.IsHttps
        );

        // Return response with tokens for backward compatibility with frontend
        return Ok(new AuthResponse
        {
            Token = response.Token, // Keep for backward compatibility
            RefreshToken = response.RefreshToken,
            Email = response.Email,
            Name = response.Name,
            AuthMethod = response.AuthMethod ?? "Local",
            Requires2FA = response.Requires2FA
        });
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token and sets secure cookies.
    /// Supports reading refresh token from either request body or httpOnly cookie.
    /// </summary>
    /// <param name="request">The refresh token request containing the refresh token (optional if using cookies).</param>

}
