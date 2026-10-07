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
    /// Open self-serve email/password registration is disabled (issue #347).
    /// New teller accounts are created via Google, or via a SuperAdmin one-time invite.
    /// Existing local password login remains.
    /// </summary>
    /// <param name="request">Ignored. The body is accepted so leftover clients get a clear i18n error.</param>
    /// <returns>400 with <c>auth.errors.openRegisterDisabled</c>.</returns>
    [HttpPost("registerAccount")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.LoginAttemptBlocked,
            Email = request.Email,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = "Open self-serve registration rejected (IdP-first; Google create path)",
            IsSuspicious = true,
            Severity = Backend.SecurityEventSeverity.Warning
        });

        return BadRequest(new { error = OpenRegisterDisabledKey });
    }

    /// <summary>
    /// Peeks a SuperAdmin one-time invite. Invalid, used, and expired tokens return
    /// <c>valid: false</c> (same shape) so the SPA can show the form or an error.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("account-invite")]
    public async Task<IActionResult> PeekAccountInvite([FromQuery] string? token)
    {
        var status = await _accountInviteService.PeekAsync(token);
        return Ok(status);
    }

    /// <summary>
    /// Creates one local email/password account using a SuperAdmin one-time invite.
    /// Open <c>registerAccount</c> stays disabled; this is the only anonymous local create path.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("registerWithInvite")]
    public async Task<IActionResult> RegisterWithInvite([FromBody] RegisterWithInviteRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var (success, error, response) = await _accountInviteService.RedeemAsync(request);

        if (!success)
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.LoginAttemptBlocked,
                Email = request.Email,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"Invite register rejected: {error}",
                IsSuspicious = string.Equals(error, AccountInviteService.InvalidInviteKey, StringComparison.Ordinal),
                Severity = SecurityEventSeverity.Warning
            });
            return BadRequest(new { error });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.AccountCreated,
            UserId = user?.Id,
            Email = request.Email,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = "Local account created via SuperAdmin invite",
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
    /// Unknown elections and wrong passcodes on an open election return the same body.
    /// Closed elections and elections with no main teller are rejected before the passcode is compared.
    /// An active per-election lockout rejects every passcode attempt.
    /// </summary>
    /// <param name="request">The teller login request containing election GUID and access code.</param>
    /// <returns>A teller authentication response with a limited JWT if successful.</returns>
    [AllowAnonymous]
    [HttpPost("teller-login")]
    public async Task<IActionResult> TellerLogin([FromBody] TellerLoginRequest request)
    {
        var clientIp = HttpContext.GetClientIpAddress();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == request.ElectionGuid);

        if (election == null)
        {
            TellerPasscodeComparer.EqualsUtf8(
                TellerPasscodeComparer.MissingElectionPlaceholder,
                request.AccessCode);
            await LogTellerLoginFailureAsync(
                clientIp,
                userAgent,
                electionGuid: null,
                details: $"Teller login failed: election not found ({request.ElectionGuid})",
                isSuspicious: false,
                severity: SecurityEventSeverity.Info);
            return InvalidElectionOrPasscode();
        }

        if (!ElectionTellerAccessHelper.IsGuestTellerAccessOpen(election.ListedForPublicAsOf))
        {
            await LogTellerLoginFailureAsync(
                clientIp,
                userAgent,
                election.ElectionGuid,
                details: $"Teller login failed: election not open for tellers ({request.ElectionGuid})",
                isSuspicious: false,
                severity: SecurityEventSeverity.Info);
            return BadRequest(new { error = TellerLoginNotOpenKey });
        }

        if (!_assignmentService.HasActiveMainTeller(request.ElectionGuid))
        {
            await LogTellerLoginFailureAsync(
                clientIp,
                userAgent,
                election.ElectionGuid,
                details: $"Teller login failed: no main teller connected ({request.ElectionGuid})",
                isSuspicious: false,
                severity: SecurityEventSeverity.Info);
            return BadRequest(new { error = TellerLoginNoMainTellerKey });
        }

        var passcodeMatches = TellerPasscodeComparer.EqualsUtf8(
            election.ElectionPasscode,
            request.AccessCode);

        if (await _tellerLoginLockoutService.IsLockedAsync(election.ElectionGuid))
        {
            return TellerLoginLocked();
        }

        if (!passcodeMatches)
        {
            var failure = await _tellerLoginLockoutService.RecordPasscodeFailureAsync(election.ElectionGuid);
            await LogTellerLoginFailureAsync(
                clientIp,
                userAgent,
                election.ElectionGuid,
                details: $"Teller login failed: invalid access code for election ({request.ElectionGuid})",
                isSuspicious: true,
                severity: SecurityEventSeverity.Warning);

            if (failure.LockoutStarted)
            {
                var lockedUntil = failure.LockedUntil?.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
                {
                    EventType = SecurityEventType.TellerLoginLocked,
                    ElectionGuid = election.ElectionGuid,
                    IpAddress = clientIp,
                    UserAgent = userAgent,
                    Details =
                        $"Guest teller login for this election is locked until {lockedUntil} UTC after {failure.ConsecutiveFailures} consecutive failed passcodes.",
                    IsSuspicious = true,
                    Severity = SecurityEventSeverity.Warning
                });
            }

            if (failure.IsLocked)
            {
                return TellerLoginLocked();
            }

            return InvalidElectionOrPasscode();
        }

        await _tellerLoginLockoutService.ResetAsync(election.ElectionGuid);

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
            ElectionGuid = election.ElectionGuid,
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

    private IActionResult InvalidElectionOrPasscode()
    {
        RateLimitingMiddleware.MarkIpFailure(HttpContext);
        return BadRequest(new { error = InvalidElectionOrPasscodeKey });
    }

    private IActionResult TellerLoginLocked()
    {
        return BadRequest(new { error = TellerLoginLockedKey });
    }

    private Task LogTellerLoginFailureAsync(
        string clientIp,
        string userAgent,
        Guid? electionGuid,
        string details,
        bool isSuspicious,
        SecurityEventSeverity severity)
    {
        return _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.TellerLoginFailure,
            ElectionGuid = electionGuid,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = details,
            IsSuspicious = isSuspicious,
            Severity = severity
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
