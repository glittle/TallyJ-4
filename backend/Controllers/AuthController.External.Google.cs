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
    [HttpGet("google/login")]
    public async Task<IActionResult> GoogleLogin([FromQuery] string? returnUrl = null)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        _logger.LogInformation("Google login attempt from {ClientIp} with returnUrl: {ReturnUrl}", clientIp, returnUrl);

        var googleClientSecret = _configuration["GoogleClientSecret"]; // server only
        var googleClientId = _configuration["ClientEnv:googleClientId"]; // client as well

        _logger.LogInformation("Google ClientId configured: {!string.IsNullOrWhiteSpace(googleClientId)}, ClientSecret configured: {!string.IsNullOrWhiteSpace(googleClientSecret)}", !string.IsNullOrWhiteSpace(googleClientId), !string.IsNullOrWhiteSpace(googleClientSecret));

        if (string.IsNullOrWhiteSpace(googleClientId) || string.IsNullOrWhiteSpace(googleClientSecret)
            || googleClientId.StartsWith("<") || googleClientSecret.StartsWith("<"))
        {
            _logger.LogWarning("Google OAuth login attempted but credentials are not configured");

            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.OAuthLoginFailure,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = "Google OAuth login attempted but not configured",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Warning
            });

            _logger.LogInformation("Returning BadRequest for Google login");
            return BadRequest(new { error = "Google authentication is not configured on this server. Please contact your administrator or use email/password login." });
        }

        var redirect = HttpContext.Request.Query["redirect"].ToString();

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback)),
            Items =
            {
                { "returnUrl", returnUrl ?? string.Empty },
                { "redirect", redirect ?? "/elections" }
            }
        };

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.OAuthLoginInitiated,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = $"Google OAuth login initiated with return URL: {returnUrl ?? "default"}",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Gets information about the currently authenticated user.
    /// </summary>

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        string? returnUrl = null;
        try
        {

            // Authenticate the external cookie explicitly
            var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
            {
                _logger.LogWarning("Google callback: Failed to authenticate external scheme");
                return Redirect(GetErrorRedirectUrl(returnUrl, "Failed to retrieve login information from Google"));
            }

            var principal = authenticateResult.Principal!;

            _logger.LogInformation("Google callback: External auth succeeded, principal: {Principal}",
                principal.Identity?.Name ?? "null");

            // Extract return URL and redirect from authentication properties
            if (authenticateResult.Properties?.Items.TryGetValue("returnUrl", out var returnUrlValue) == true)
            {
                returnUrl = returnUrlValue;
            }

            string redirectUrl;
            if (authenticateResult.Properties?.Items.TryGetValue("redirect", out var redirectValue) == true && !string.IsNullOrEmpty(redirectValue))
            {
                redirectUrl = redirectValue.StartsWith("http") ? redirectValue : $"https://localhost:8095{redirectValue}";
            }
            else
            {
                redirectUrl = "https://localhost:8095/elections";
            }

            // Extract claims directly from the authenticated principal
            var email = principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Google callback: Email claim is missing from Google response");
                return Redirect(GetErrorRedirectUrl(returnUrl, "Email not provided by Google"));
            }

            var googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var displayName = principal.FindFirstValue(ClaimTypes.Name) ??
                              principal.FindFirstValue(ClaimTypes.GivenName);

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            var (user, _) = await ProcessGoogleUserAsync(
                email,
                googleId!,
                displayName,
                clientIp,
                userAgent,
                $"Google OAuth login successful for user {email}"
            );

            // Clean up the external authentication cookie
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            _logger.LogInformation("Google callback: Successfully authenticated user {Email}, redirecting to {Url}", email, redirectUrl);
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google callback: Unexpected error during Google authentication");
            return Redirect(GetErrorRedirectUrl(returnUrl, "An unexpected error occurred during authentication"));
        }
    }

    /// <summary>
    /// Authenticates a user via Google One Tap by validating a Google ID token credential.
    /// </summary>
    /// <param name="request">The request containing the Google ID token credential.</param>
    /// <returns>The authentication response with user info if successful, or an error if validation fails.</returns>
    [HttpPost("google/one-tap")]
    public async Task<IActionResult> GoogleOneTap([FromBody] GoogleOneTapRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var googleClientId = _configuration["ClientEnv:googleClientId"];
        if (string.IsNullOrWhiteSpace(googleClientId) || googleClientId.StartsWith("<"))
        {
            _logger.LogWarning("Google One Tap attempted but Google Client ID is not configured");
            return BadRequest(new { error = "Google authentication is not configured on this server." });
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleClientId }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, settings);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Google One Tap: Invalid Google ID token");

            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.OAuthLoginFailure,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = "Google One Tap: Invalid ID token",
                IsSuspicious = true,
                Severity = SecurityEventSeverity.Warning
            });

            return BadRequest(new { error = "Invalid Google credential." });
        }

        var email = payload.Email;
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new { error = "Email not provided by Google." });
        }

        var googleId = payload.Subject;
        var displayName = payload.Name ?? payload.GivenName;

        try
        {
            var (user, _) = await ProcessGoogleUserAsync(
                email,
                googleId,
                displayName,
                clientIp,
                userAgent,
                $"Google One Tap login successful for user {email}"
            );

            return Ok(new AuthResponse
            {
                Token = null,
                RefreshToken = null,
                Email = user.Email!,
                Name = user.DisplayName,
                AuthMethod = "Google",
                Requires2FA = false
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Google One Tap: Error processing user {Email}", email);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates a user using the Telegram Login Widget for officer / teller flows.
    /// </summary>

    private async Task<(AppUser user, bool isNewUser)> ProcessExternalUserAsync(
        string email,
        string provider,
        string providerKey,
        string? displayName,
        string? clientIp,
        string? userAgent,
        string eventDetails)
    {
        var user = await _userManager.FindByEmailAsync(email);
        bool isNewUser = false;

        if (user == null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                AuthMethod = provider
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Failed to create user {Email}: {Errors}",
                    email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                throw new InvalidOperationException("Failed to create user account.");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Officer");
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning("Failed to assign Officer role to user {Email}: {Errors}",
                    email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }

            _logger.LogInformation("Created new user {Email} with {Provider} authentication", email, provider);
            isNewUser = true;
        }
        else
        {
            if (!string.IsNullOrEmpty(displayName) && string.IsNullOrEmpty(user.DisplayName))
            {
                user.DisplayName = displayName;
                await _userManager.UpdateAsync(user);
            }
        }

        var logins = await _userManager.GetLoginsAsync(user);
        if (!logins.Any(l => l.LoginProvider == provider && l.ProviderKey == providerKey))
        {
            var addLoginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
            if (!addLoginResult.Succeeded)
            {
                _logger.LogWarning("Failed to add {Provider} login to user {Email}: {Errors}",
                    provider, email, string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
            }

            var methods = string.IsNullOrEmpty(user.AuthMethod) ? new List<string>() : new List<string>(user.AuthMethod.Split(','));
            if (!methods.Contains(provider))
            {
                methods.Add(provider);
                user.AuthMethod = string.Join(",", methods);
                await _userManager.UpdateAsync(user);
            }
        }

        var token = _jwtTokenService.GenerateToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = _jwtTokenService.CreateRefreshToken(user.Id, refreshToken);
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        SecureCookieMiddleware.SetAuthCookies(
            HttpContext,
            token,
            refreshToken,
            user.Email!,
            user.DisplayName,
            user.AuthMethod ?? provider,
            HttpContext.Request.IsHttps
        );

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.OAuthLoginSuccess,
            UserId = user.Id,
            Email = email,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = eventDetails,
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        await _remoteLogService.SendLogAsync($"FullTeller login via {provider}", user.DisplayName ?? user.Email, null);

        return (user, isNewUser);
    }
    private async Task<(AppUser user, bool isNewUser)> ProcessGoogleUserAsync(
        string email,
        string googleId,
        string? displayName,
        string? clientIp,
        string? userAgent,
        string eventDetails)
    {
        var user = await _userManager.FindByEmailAsync(email);
        bool isNewUser = false;

        if (user == null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                GoogleId = googleId,
                AuthMethod = "Google"
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Failed to create user {Email}: {Errors}",
                    email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                throw new InvalidOperationException("Failed to create user account.");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Officer");
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning("Failed to assign Officer role to user {Email}: {Errors}",
                    email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }

            _logger.LogInformation("Created new user {Email} with Google authentication", email);
            isNewUser = true;
            await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", googleId, "Google"));
        }
        else
        {
            if (string.IsNullOrEmpty(user.GoogleId))
            {
                user.GoogleId = googleId;
                user.AuthMethod = "Google";
                if (!string.IsNullOrEmpty(displayName))
                {
                    user.DisplayName = displayName;
                }
                await _userManager.UpdateAsync(user);
                await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", googleId, "Google"));
                _logger.LogInformation("Linked Google account to existing user {Email}", email);
            }
            else if (!string.IsNullOrEmpty(displayName) && string.IsNullOrEmpty(user.DisplayName))
            {
                user.DisplayName = displayName;
                await _userManager.UpdateAsync(user);
            }
        }

        var token = _jwtTokenService.GenerateToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = _jwtTokenService.CreateRefreshToken(user.Id, refreshToken);
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        SecureCookieMiddleware.SetAuthCookies(
            HttpContext,
            token,
            refreshToken,
            user.Email!,
            user.DisplayName,
            user.AuthMethod ?? "Google",
            HttpContext.Request.IsHttps
        );

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.OAuthLoginSuccess,
            UserId = user.Id,
            Email = email,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = eventDetails,
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        await _remoteLogService.SendLogAsync($"FullTeller login via Google" + (isNewUser ? " (new user)" : ""), user.DisplayName ?? user.Email, null);

        return (user, isNewUser);
    }


}
