using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    /// HttpOnly cookie holding the online-voter JWT. Distinct from <see cref="AccessTokenCookieName"/>
    /// so teller and voter sessions can coexist in the same browser.
    /// </summary>
    public const string VoterTokenCookieName = "voter_token";

    /// <summary>
    /// Non-httpOnly flag cookie so the SPA can detect an online-voter session without reading the JWT.
    /// Value is always <c>1</c> — not a secret and not PII.
    /// </summary>
    public const string VoterSessionCookieName = "voter_session";

    /// <summary>
    /// Default lifetime for voter session cookies, matching the 24-hour online-voter JWT.
    /// </summary>
    public const int VoterSessionExpiryMinutes = 24 * 60;

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
    /// <param name="accessTokenExpiryMinutes">Optional override for access-token cookie lifetime (defaults to Jwt:ExpiryMinutes).</param>
    public static void SetAuthCookies(
        HttpContext context,
        string accessToken,
        string refreshToken,
        string email,
        string? name,
        string authMethod,
        bool isHttps = true,
        int? accessTokenExpiryMinutes = null)
    {
        var accessTokenLifetimeMinutes = accessTokenExpiryMinutes
            ?? ResolveAccessTokenExpiryMinutes(context);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(accessTokenLifetimeMinutes),
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

    private static int ResolveAccessTokenExpiryMinutes(HttpContext context)
    {
        var configuration = context.RequestServices?.GetService<IConfiguration>();
        return int.Parse(configuration?["Jwt:ExpiryMinutes"] ?? "60");
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
    /// True for HTTP and SignalR paths that must authenticate as an online voter, not a teller.
    /// Used so <c>voter_token</c> and <c>auth_token</c> can both be present without mix-up.
    /// </summary>
    public static bool IsVoterScopedPath(PathString path)
    {
        return path.StartsWithSegments("/api/online-voting")
               || path.StartsWithSegments("/hubs/all-voters")
               || path.StartsWithSegments("/hubs/voter-personal");
    }

    /// <summary>
    /// Sets httpOnly <c>voter_token</c> plus a non-httpOnly <c>voter_session</c> flag.
    /// Cookie attributes match teller auth (<c>HttpOnly</c>/<c>Secure</c>/<c>SameSite</c>/<c>Path</c>/<c>Domain</c>).
    /// </summary>
    public static void SetVoterAuthCookies(
        HttpContext context,
        string accessToken,
        bool isHttps = true,
        int? accessTokenExpiryMinutes = null)
    {
        var lifetimeMinutes = accessTokenExpiryMinutes ?? VoterSessionExpiryMinutes;
        var expires = DateTimeOffset.UtcNow.AddMinutes(lifetimeMinutes);

        var tokenOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = expires,
            Path = "/",
            Domain = isHttps ? null : "localhost"
        };

        context.Response.Cookies.Append(VoterTokenCookieName, accessToken, tokenOptions);

        var sessionFlagOptions = new CookieOptions
        {
            HttpOnly = false,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = expires,
            Path = "/",
            Domain = isHttps ? null : "localhost"
        };

        context.Response.Cookies.Append(VoterSessionCookieName, "1", sessionFlagOptions);
    }

    /// <summary>
    /// Clears online-voter cookies without touching teller auth cookies.
    /// </summary>
    public static void ClearVoterAuthCookies(HttpContext context)
    {
        var isHttps = context.Request.IsHttps;
        var expiredTokenOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            MaxAge = TimeSpan.Zero,
            Path = "/",
            Domain = isHttps ? null : "localhost"
        };

        context.Response.Cookies.Append(VoterTokenCookieName, "", expiredTokenOptions);

        var expiredFlagOptions = new CookieOptions
        {
            HttpOnly = false,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            MaxAge = TimeSpan.Zero,
            Path = "/",
            Domain = isHttps ? null : "localhost"
        };

        context.Response.Cookies.Append(VoterSessionCookieName, "", expiredFlagOptions);
    }

    /// <summary>
    /// Gets the online-voter JWT from cookies.
    /// </summary>
    public static string? GetVoterAccessToken(HttpContext context)
    {
        return context.Request.Cookies[VoterTokenCookieName];
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


