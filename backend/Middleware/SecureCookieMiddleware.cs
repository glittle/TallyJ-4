using System;
using Microsoft.AspNetCore.Http;

namespace Backend.Middleware;

/// <summary>
/// Middleware for handling secure cookie-based authentication tokens.
/// Provides methods to set and read httpOnly secure cookies for JWT tokens.
/// </summary>
public class SecureCookieMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Cookie name for the access token.
    /// </summary>
    public const string AccessTokenCookieName = "auth_token";

    /// <summary>
    /// Cookie name for the refresh token.
    /// </summary>
    public const string RefreshTokenCookieName = "refresh_token";

    /// <summary>
    /// Cookie name for the user email.
    /// </summary>
    public const string UserEmailCookieName = "user_email";

    /// <summary>
    /// Cookie name for the user name.
    /// </summary>
    public const string UserNameCookieName = "user_name";

    /// <summary>
    /// Cookie name for the auth method.
    /// </summary>
    public const string AuthMethodCookieName = "auth_method";

    /// <summary>
    /// Initializes a new instance of the <see cref="SecureCookieMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public SecureCookieMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
    }

    /// <summary>
    /// Sets secure httpOnly cookies for authentication data.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="accessToken">The JWT access token.</param>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="email">The user's email.</param>
    /// <param name="name">The user's display name.</param>
    /// <param name="authMethod">The authentication method.</param>
    /// <param name="isHttps">Whether the request is over HTTPS.</param>
    public static void SetAuthCookies(
        HttpContext context,
        string accessToken,
        string refreshToken,
        string email,
        string? name,
        string authMethod,
        bool isHttps = true)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(15), // Access token expiry
            Path = "/",
            Domain = isHttps ? null : "localhost" // Share cookies across localhost ports in dev
        };

        context.Response.Cookies.Append(AccessTokenCookieName, accessToken, cookieOptions);

        // Refresh token cookie with longer expiry
        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30), // Refresh token expiry
            Path = "/",
            Domain = isHttps ? null : "localhost"
        };

        context.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, refreshCookieOptions);

        // User info cookies (not httpOnly for client access)
        var userCookieOptions = new CookieOptions
        {
            HttpOnly = false,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            Path = "/",
            Domain = isHttps ? null : "localhost"
        };

        context.Response.Cookies.Append(UserEmailCookieName, email, userCookieOptions);
        if (!string.IsNullOrEmpty(name))
        {
            context.Response.Cookies.Append(UserNameCookieName, name, userCookieOptions);
        }
        context.Response.Cookies.Append(AuthMethodCookieName, authMethod, userCookieOptions);
    }

    /// <summary>
    /// Clears all authentication cookies.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public static void ClearAuthCookies(HttpContext context)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = context.Request.IsHttps ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(-1), // Expire immediately
            MaxAge = TimeSpan.Zero,
            Path = "/",
            Domain = context.Request.IsHttps ? null : "localhost"
        };

        context.Response.Cookies.Append(AccessTokenCookieName, "", cookieOptions);
        context.Response.Cookies.Append(RefreshTokenCookieName, "", cookieOptions);

        var userCookieOptions = new CookieOptions
        {
            HttpOnly = false,
            Secure = context.Request.IsHttps,
            SameSite = context.Request.IsHttps ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            MaxAge = TimeSpan.Zero,
            Path = "/",
            Domain = context.Request.IsHttps ? null : "localhost"
        };

        context.Response.Cookies.Append(UserEmailCookieName, "", userCookieOptions);
        context.Response.Cookies.Append(UserNameCookieName, "", userCookieOptions);
        context.Response.Cookies.Append(AuthMethodCookieName, "", userCookieOptions);
    }

    /// <summary>
    /// Gets the access token from cookies.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The access token if present, null otherwise.</returns>
    public static string? GetAccessToken(HttpContext context)
    {
        return context.Request.Cookies[AccessTokenCookieName];
    }

    /// <summary>
    /// Gets the refresh token from cookies.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The refresh token if present, null otherwise.</returns>
    public static string? GetRefreshToken(HttpContext context)
    {
        return context.Request.Cookies[RefreshTokenCookieName];
    }

    /// <summary>
    /// Gets the user email from cookies.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The user email if present, null otherwise.</returns>
    public static string? GetUserEmail(HttpContext context)
    {
        var value = context.Request.Cookies[UserEmailCookieName];
        return value != null ? Uri.UnescapeDataString(value) : null;
    }

    /// <summary>
    /// Gets the user name from cookies.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The user name if present, null otherwise.</returns>
    public static string? GetUserName(HttpContext context)
    {
        var value = context.Request.Cookies[UserNameCookieName];
        return value != null ? Uri.UnescapeDataString(value) : null;
    }

    /// <summary>
    /// Gets the auth method from cookies.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The auth method if present, null otherwise.</returns>
    public static string? GetAuthMethod(HttpContext context)
    {
        var value = context.Request.Cookies[AuthMethodCookieName];
        return value != null ? Uri.UnescapeDataString(value) : null;
    }
}


