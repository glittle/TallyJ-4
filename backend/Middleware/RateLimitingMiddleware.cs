using System.Net;
using Backend;
using Backend.DTOs.Security;
using Backend.Helpers;
using Backend.Services;

namespace Backend.Middleware;

/// <summary>
/// In-memory rate limiting for anonymous teller and voter authentication endpoints.
/// Keys by Connection.RemoteIpAddress after UseForwardedHeaders (see GetClientIpAddress).
/// </summary>
public class RateLimitingMiddleware
{
    public const string TooManyRequestsKey = "error.tooManyRequests";

    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitStore _store;

    // Rate limits: key = endpoint path (case-insensitive), value = (max requests, time window)
    private static readonly Dictionary<string, (int MaxRequests, TimeSpan Window)> RateLimits =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "/api/auth/login", (5, TimeSpan.FromMinutes(1)) },
            { "/api/auth/registerAccount", (3, TimeSpan.FromHours(1)) },
            { "/api/auth/verify2fa", (10, TimeSpan.FromMinutes(1)) },
            { "/api/auth/forgotPassword", (3, TimeSpan.FromHours(1)) },
            { "/api/auth/resetPassword", (3, TimeSpan.FromHours(1)) },
            { "/api/auth/google/one-tap", (5, TimeSpan.FromMinutes(1)) },
            { "/api/auth/facebook", (5, TimeSpan.FromMinutes(1)) },
            { "/api/auth/kakao", (5, TimeSpan.FromMinutes(1)) },
            { "/api/auth/telegram", (5, TimeSpan.FromMinutes(1)) },
            { "/api/online-voting/requestCode", (5, TimeSpan.FromMinutes(1)) },
            { "/api/online-voting/verifyCode", (5, TimeSpan.FromMinutes(1)) },
            { "/api/online-voting/googleAuth", (5, TimeSpan.FromMinutes(1)) },
            { "/api/online-voting/facebookAuth", (5, TimeSpan.FromMinutes(1)) },
            { "/api/online-voting/kakaoAuth", (5, TimeSpan.FromMinutes(1)) },
            { "/api/online-voting/telegramAuth", (5, TimeSpan.FromMinutes(1)) }
        };

    /// <summary>
    /// Initializes a new instance of the RateLimitingMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="store">The rate limit store for persisting request counts.</param>
    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, RateLimitStore store)
    {
        _next = next;
        _logger = logger;
        _store = store;
    }

    /// <summary>
    /// Processes the HTTP request and applies rate limiting if configured for the endpoint.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <param name="securityAuditService">The security audit service for logging rate limit violations.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, ISecurityAuditService securityAuditService)
    {
        var path = NormalizePath(context.Request.Path.Value);
        if (path != null && RateLimits.TryGetValue(path, out var limit))
        {
            var clientIp = context.GetClientIpAddress();
            var clientKey = GetClientKey(path, clientIp);
            var userAgent = context.Request.Headers.UserAgent.ToString();

            CleanupOldRequests(clientKey, limit.Window);

            var requests = _store.RequestLog.GetOrAdd(clientKey, _ => new List<DateTime>());
            if (requests.Count >= limit.MaxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for {Path} by client {ClientKey}", path, clientKey);

                await securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
                {
                    EventType = SecurityEventType.RateLimitExceeded,
                    IpAddress = clientIp,
                    UserAgent = userAgent,
                    Details = $"Rate limit exceeded for {path} - {requests.Count} requests in {limit.Window.TotalMinutes} minutes",
                    IsSuspicious = true,
                    Severity = SecurityEventSeverity.Warning
                });

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync($"{{\"error\":\"{TooManyRequestsKey}\"}}");
                return;
            }

            requests.Add(DateTime.UtcNow);
        }

        await _next(context);
    }

    internal static string GetClientKey(string path, string clientIp)
    {
        return $"{path.ToLowerInvariant()}_{clientIp}";
    }

    internal static string? NormalizePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        return path.TrimEnd('/');
    }

    private void CleanupOldRequests(string clientKey, TimeSpan window)
    {
        if (_store.RequestLog.TryGetValue(clientKey, out var requests))
        {
            var cutoff = DateTime.UtcNow - window;
            requests.RemoveAll(r => r < cutoff);

            if (requests.Count == 0)
            {
                _store.RequestLog.TryRemove(clientKey, out _);
            }
        }
    }
}
