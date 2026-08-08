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
    [HttpPost("refreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request)
    {
        // Try to get refresh token from request body first, then fall back to cookie
        var refreshTokenValue = request?.RefreshToken;

        if (string.IsNullOrEmpty(refreshTokenValue))
        {
            // Try to get from cookie
            refreshTokenValue = HttpContext.Request.Cookies["refresh_token"];
        }

        if (string.IsNullOrEmpty(refreshTokenValue))
        {
            return BadRequest(new { error = "Refresh token is required" });
        }

        var tokenHash = _jwtTokenService.HashRefreshToken(refreshTokenValue);
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && !rt.IsRevoked);

        if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new { error = "Invalid or expired refresh token" });
        }

        // Generate new tokens
        var user = await _context.Users.FindAsync(refreshToken.UserId);
        if (user == null)
        {
            return BadRequest(new { error = "User not found" });
        }

        var newToken = _jwtTokenService.GenerateToken(user);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenEntity = _jwtTokenService.CreateRefreshToken(user.Id, newRefreshToken);

        // Revoke old refresh token
        refreshToken.IsRevoked = true;
        refreshToken.RevokedReason = "Replaced by new token";
        refreshToken.ReplacedByToken = newRefreshToken;

        _context.RefreshTokens.Add(newRefreshTokenEntity);
        await _context.SaveChangesAsync();

        var authMethod = user.AuthMethod ?? "Local";

        // Set secure cookies with new tokens
        SecureCookieMiddleware.SetAuthCookies(
            HttpContext,
            newToken,
            newRefreshToken,
            user.Email!,
            user.DisplayName,
            authMethod,
            HttpContext.Request.IsHttps
        );

        return Ok(new AuthResponse
        {
            Token = null, // Not returned - stored in httpOnly cookie
            RefreshToken = null, // Not returned - stored in httpOnly cookie
            Email = user.Email!,
            Name = user.DisplayName,
            AuthMethod = authMethod,
            Requires2FA = false
        });
    }

    /// <summary>
    /// Gets the roles assigned to the authenticated user.
    /// </summary>
    /// <returns>The user's roles and basic information.</returns>

}
