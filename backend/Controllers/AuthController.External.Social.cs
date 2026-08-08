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
    [HttpPost("telegram")]
    public async Task<IActionResult> TelegramLogin([FromBody] TelegramLoginRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var botToken = _configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
        {
            _logger.LogWarning("Telegram login attempted but Telegram bot token is not configured");
            return BadRequest(new { error = "Telegram authentication is not configured on this server." });
        }

        if (!ValidateTelegramHash(request, botToken))
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.OAuthLoginFailure,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = "Telegram login: invalid hash",
                IsSuspicious = true,
                Severity = SecurityEventSeverity.Warning
            });

            return BadRequest(new { error = "Invalid Telegram data." });
        }

        var authAgeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - request.AuthDate;
        if (authAgeSeconds > 86400)
        {
            return BadRequest(new { error = "Telegram login request expired." });
        }

        try
        {
            var (user, _) = await ProcessTelegramUserAsync(request, clientIp, userAgent);

            return Ok(new AuthResponse
            {
                Token = null,
                RefreshToken = null,
                Email = user.Email!,
                Name = user.DisplayName,
                AuthMethod = "Telegram",
                Requires2FA = false
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Telegram login: error processing user {TelegramId}", request.Id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates a user using Facebook for officer / teller flows.
    /// </summary>
    [HttpPost("facebook")]
    public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        try
        {
            var client = _httpClientFactory.CreateClient("Facebook");
            using var fbRequest = new HttpRequestMessage(HttpMethod.Get, "/me?fields=id,email,name");
            fbRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);
            var response = await client.SendAsync(fbRequest);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Facebook API returned non-success for teller auth: {Status}", response.StatusCode);
                return BadRequest(new { error = "Invalid Facebook token." });
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("email", out var emailElement))
            {
                return BadRequest(new { error = "Email not provided by Facebook." });
            }

            var email = emailElement.GetString();
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { error = "Email not provided by Facebook." });
            }

            var fbId = doc.RootElement.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
            var displayName = doc.RootElement.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;

            if (string.IsNullOrEmpty(fbId))
            {
                return BadRequest(new { error = "ID not provided by Facebook." });
            }

            var (user, _) = await ProcessExternalUserAsync(
                email,
                "Facebook",
                fbId,
                displayName,
                clientIp,
                userAgent,
                $"Facebook login successful for user {email}"
            );

            return Ok(new AuthResponse
            {
                Token = null,
                RefreshToken = null,
                Email = user.Email!,
                Name = user.DisplayName,
                AuthMethod = "Facebook",
                Requires2FA = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Facebook login: error processing user");
            return BadRequest(new { error = "Error authenticating with Facebook." });
        }
    }

    /// <summary>
    /// Authenticates a user using Kakao for officer / teller flows.
    /// </summary>
    [HttpPost("kakao")]
    public async Task<IActionResult> KakaoLogin([FromBody] KakaoLoginRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        try
        {
            var client = _httpClientFactory.CreateClient("Kakao");
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/v2/user/me");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);

            var response = await client.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Kakao API returned non-success for teller auth: {Status}", response.StatusCode);
                return BadRequest(new { error = "Invalid Kakao token." });
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            string? email = null;
            string? displayName = null;
            var kakaoId = doc.RootElement.TryGetProperty("id", out var idElement) ? idElement.GetInt64().ToString() : null;

            if (doc.RootElement.TryGetProperty("kakao_account", out var account))
            {
                if (account.TryGetProperty("email", out var emailEl))
                    email = emailEl.GetString();

                if (account.TryGetProperty("profile", out var profile) && profile.TryGetProperty("nickname", out var nameEl))
                    displayName = nameEl.GetString();
            }

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { error = "Email not provided by Kakao." });
            }
            if (string.IsNullOrEmpty(kakaoId))
            {
                return BadRequest(new { error = "ID not provided by Kakao." });
            }

            var (user, _) = await ProcessExternalUserAsync(
                email,
                "Kakao",
                kakaoId,
                displayName,
                clientIp,
                userAgent,
                $"Kakao login successful for user {email}"
            );

            return Ok(new AuthResponse
            {
                Token = null,
                RefreshToken = null,
                Email = user.Email!,
                Name = user.DisplayName,
                AuthMethod = "Kakao",
                Requires2FA = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kakao login: error processing user");
            return BadRequest(new { error = "Error authenticating with Kakao." });
        }
    }


    private async Task<(AppUser user, bool isNewUser)> ProcessTelegramUserAsync(
        TelegramLoginRequest request,
        string? clientIp,
        string? userAgent)
    {
        var telegramId = request.Id.ToString();
        var displayName = $"{request.FirstName} {request.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = request.Username ?? $"Telegram {telegramId}";
        }

        var email = !string.IsNullOrWhiteSpace(request.Username)
            ? $"{request.Username}@telegram.local"
            : $"telegram_{telegramId}@telegram.local";

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId);
        bool isNewUser = false;

        if (user == null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                TelegramId = telegramId,
                AuthMethod = "Telegram"
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Failed to create Telegram user {TelegramId}: {Errors}",
                    telegramId, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                throw new InvalidOperationException("Failed to create user account.");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Officer");
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning("Failed to assign Officer role to Telegram user {TelegramId}: {Errors}",
                    telegramId, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }

            _logger.LogInformation("Created new user for Telegram ID {TelegramId}", telegramId);
            isNewUser = true;
        }
        else
        {
            var needsUpdate = false;
            if (string.IsNullOrEmpty(user.TelegramId))
            {
                user.TelegramId = telegramId;
                needsUpdate = true;
                await _userManager.AddLoginAsync(user, new UserLoginInfo("Telegram", telegramId, "Telegram"));
            }

            if (string.IsNullOrEmpty(user.DisplayName) && !string.IsNullOrEmpty(displayName))
            {
                user.DisplayName = displayName;
                needsUpdate = true;
            }

            user.AuthMethod = "Telegram";

            if (needsUpdate)
            {
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
            user.AuthMethod ?? "Telegram",
            HttpContext.Request.IsHttps
        );

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.OAuthLoginSuccess,
            UserId = user.Id,
            Email = user.Email,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = $"Telegram login successful for Telegram ID {telegramId}",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        await _remoteLogService.SendLogAsync($"FullTeller login via Telegram" + (isNewUser ? " (new user)" : ""), user.DisplayName ?? user.Email, null);

        return (user, isNewUser);
    }

    private static string BuildTelegramDataCheckString(TelegramLoginRequest request)
    {
        var fields = new SortedDictionary<string, string>
        {
            ["auth_date"] = request.AuthDate.ToString(),
            ["first_name"] = request.FirstName,
            ["id"] = request.Id.ToString()
        };

        if (!string.IsNullOrEmpty(request.LastName)) fields["last_name"] = request.LastName;
        if (!string.IsNullOrEmpty(request.PhotoUrl)) fields["photo_url"] = request.PhotoUrl;
        if (!string.IsNullOrEmpty(request.Username)) fields["username"] = request.Username;

        return string.Join("\n", fields.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    private static string ComputeTelegramHash(string dataCheckString, string botToken)
    {
        using var sha256 = SHA256.Create();
        var secretKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(botToken));

        using var hmac = new HMACSHA256(secretKey);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
        return Convert.ToHexString(computedHash).ToLowerInvariant();
    }

    private bool ValidateTelegramHash(TelegramLoginRequest request, string botToken)
    {
        var dataCheckString = BuildTelegramDataCheckString(request);
        var expectedHash = ComputeTelegramHash(dataCheckString, botToken);
        return string.Equals(expectedHash, request.Hash, StringComparison.OrdinalIgnoreCase);
    }


}
