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
    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">The registration request containing user details.</param>
    /// <returns>The authentication response if successful, or an error if registration fails.</returns>
    [HttpPost("registerAccount")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var (success, error, response) = await _localAuthService.RegisterAsync(request);

        if (!success)
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.AccountCreated,
                Email = request.Email,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"Registration failed: {error}",
                IsSuspicious = false,
                Severity = Backend.SecurityEventSeverity.Info
            });
            return BadRequest(new { error });
        }

        // Get the user ID for logging
        var user = await _userManager.FindByEmailAsync(request.Email);
        var userId = user?.Id;

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.AccountCreated,
            UserId = userId,
            Email = request.Email,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = "User account created successfully",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        return Ok(response);
    }

    /// <summary>
    /// Authenticates a user and sets secure cookies with access tokens.
    /// </summary>
    /// <param name="request">The login request containing email and password.</param>
    /// <returns>The authentication response with user info if successful, or an error if login fails.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var (success, error, response) = await _localAuthService.LoginAsync(request);

        if (!success)
        {
            // Determine if this is a suspicious login attempt
            var isSuspicious = error?.Contains("locked") == true || error?.Contains("invalid") == true;
            var severity = isSuspicious ? SecurityEventSeverity.Warning : SecurityEventSeverity.Info;

            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.LoginFailure,
                Email = request.Email,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"Login failed: {error}",
                IsSuspicious = isSuspicious,
                Severity = severity
            });

            return BadRequest(new { error });
        }

        // Get user ID for successful login logging
        var user = await _userManager.FindByEmailAsync(request.Email);
        var userId = user?.Id;

        // Successful login
        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.LoginSuccess,
            UserId = userId,
            Email = request.Email,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = response?.Requires2FA == true ? "Login successful, 2FA required" : "Login successful",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        // Only set cookies if 2FA is not required
        if (response != null && !response.Requires2FA && !string.IsNullOrEmpty(response.Token))
        {
            // Set secure cookies instead of returning tokens in response
            SecureCookieMiddleware.SetAuthCookies(
                HttpContext,
                response.Token,
                response.RefreshToken ?? "",
                response.Email,
                response.Name,
                response.AuthMethod ?? "Local",
                HttpContext.Request.IsHttps
            );
        }

        // Return response without tokens (tokens are in httpOnly cookies)
        return Ok(new AuthResponse
        {
            Token = null, // Not returned - stored in httpOnly cookie
            RefreshToken = null, // Not returned - stored in httpOnly cookie
            Email = response?.Email ?? "",
            Name = response?.Name,
            AuthMethod = response?.AuthMethod ?? "Local",
            Requires2FA = response?.Requires2FA ?? false
        });
    }

    /// <summary>
    /// Authenticates a GuestTeller using an election access code.
    /// </summary>
    /// <param name="request">The teller login request containing election GUID and access code.</param>
    /// <returns>A teller authentication response with a limited JWT if successful.</returns>
    [AllowAnonymous]
    [HttpPost("teller-login")]
    public async Task<IActionResult> TellerLogin([FromBody] TellerLoginRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == request.ElectionGuid);

        if (election == null)
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.TellerLoginFailure,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"Teller login failed: election not found ({request.ElectionGuid})",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Info
            });
            return BadRequest(new { error = "Invalid election or access code" });
        }

        if (!ElectionTellerAccessHelper.IsGuestTellerAccessOpen(election.ListedForPublicAsOf))
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.TellerLoginFailure,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"Teller login failed: election not open for tellers ({request.ElectionGuid})",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Info
            });
            return BadRequest(new { error = "This election is not currently open for teller access" });
        }

        if (!_assignmentService.HasActiveMainTeller(request.ElectionGuid))
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.TellerLoginFailure,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"Teller login failed: no main teller connected ({request.ElectionGuid})",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Info
            });
            return BadRequest(new { error = "No main teller is currently connected to this election" });
        }

        if (string.IsNullOrEmpty(election.ElectionPasscode) ||
            !string.Equals(election.ElectionPasscode, request.AccessCode, StringComparison.Ordinal))
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.TellerLoginFailure,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"Teller login failed: invalid access code for election ({request.ElectionGuid})",
                IsSuspicious = true,
                Severity = SecurityEventSeverity.Warning
            });
            return BadRequest(new { error = "Invalid election or access code" });
        }

        var token = _jwtTokenService.GenerateTellerToken(election.ElectionGuid);

        SecureCookieMiddleware.SetAuthCookies(
            HttpContext,
            token,
            "",
            "",
            "Teller",
            "AccessCode",
            HttpContext.Request.IsHttps,
            accessTokenExpiryMinutes: JwtTokenService.TellerTokenExpiryHours * 60
        );

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.TellerLoginSuccess,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = $"Teller login successful for election ({request.ElectionGuid})",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        return Ok(new TellerLoginResponse
        {
            ElectionGuid = election.ElectionGuid,
            ElectionName = election.Name
        });
    }

    /// <summary>
    /// Initiates a password reset by sending a reset email to the user.
    /// </summary>
    /// <param name="request">The forgot password request containing the user's email.</param>

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "Not authenticated" });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        // Advertise super-admin only when true (omit property otherwise — reduces client exposure).
        var isSuperAdmin = !string.IsNullOrEmpty(user.Email)
            && _superAdminSettings.Emails.Any(e => string.Equals(e, user.Email, StringComparison.OrdinalIgnoreCase));

        return Ok(new CurrentUserDto
        {
            Email = user.Email,
            Name = user.DisplayName,
            AuthMethod = user.AuthMethod,
            IsSuperAdmin = isSuperAdmin
        });
    }

    /// <summary>
    /// Handles the OAuth callback from Google with state parameter validation for CSRF protection.
    /// </summary>

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.Logout,
            UserId = userId,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = "User logged out",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        SecureCookieMiddleware.ClearAuthCookies(HttpContext);

        return Ok(new { message = "Logged out successfully" });
    }

}
