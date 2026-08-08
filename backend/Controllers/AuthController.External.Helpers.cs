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
    private string GetFrontendUrl(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Uri.TryCreate(returnUrl, UriKind.Absolute, out _))
        {
            return returnUrl;
        }

        return FrontendUrlResolver.Build(_configuration, _hostEnvironment, "/auth/google/callback");
    }

    /// <summary>
    /// Logs out the current user by clearing authentication cookies.
    /// </summary>

    private string GetErrorRedirectUrl(string? returnUrl, string errorMessage)
    {
        var frontendUrl = GetFrontendUrl(returnUrl);
        var baseUrl = frontendUrl.Split('?')[0].Replace("/auth/google/callback", "/login");
        return $"{baseUrl}?error={Uri.EscapeDataString(errorMessage)}&mode=officer";
    }

    /// <summary>
    /// Generates a cryptographically secure random code verifier for PKCE (Proof Key for Code Exchange).
    /// </summary>
    /// <returns>A base64url-encoded random string between 43-128 characters.</returns>
    public static string GenerateCodeVerifier()
    {
        var bytes = new byte[32]; // 256 bits
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Generates a code challenge from a code verifier using SHA256 hashing.
    /// </summary>
    /// <param name="codeVerifier">The code verifier to hash.</param>
    /// <returns>A base64url-encoded SHA256 hash of the code verifier.</returns>
    public static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(codeVerifier);
        var hash = sha256.ComputeHash(bytes);
        return Base64UrlEncode(hash);
    }

    /// <summary>
    /// Encodes a byte array to base64url format (URL-safe base64).
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A base64url-encoded string.</returns>
    public static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }


}
