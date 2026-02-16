using System.Collections.Concurrent;
using System.Net;
using Backend.Domain;
using Backend.DTOs.Security;
using Backend.Services;

namespace Backend.Middleware;

/// <summary>
/// Simple in-memory rate limiting middleware for authentication endpoints.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    // In-memory storage for rate limiting (in production, use Redis or database)
    private static readonly ConcurrentDictionary<string, List<DateTime>> _requestLog = new();

    // Rate limits: key = endpoint, value = (max requests, time window)
    private static readonly Dictionary<string, (int MaxRequests, TimeSpan Window)> _rateLimits = new()
    {
        { "/api/auth/login", (5, TimeSpan.FromMinutes(1)) },
        { "/api/auth/registerAccount", (3, TimeSpan.FromHours(1)) },
        { "/api/auth/verify2fa", (10, TimeSpan.FromMinutes(1)) },
        { "/api/auth/forgotPassword", (3, TimeSpan.FromHours(1)) },
        { "/api/auth/resetPassword", (3, TimeSpan.FromHours(1)) }
    };

    /// <summary>
    /// Initializes a new instance of the RateLimitingMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // Note: SecurityAuditService is resolved per-request to avoid circular dependencies

    /// <summary>
    /// Processes the HTTP request and applies rate limiting if configured for the endpoint.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <param name="securityAuditService">The security audit service for logging rate limit violations.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, ISecurityAuditService securityAuditService)
    {
        var path = context.Request.Path.Value;
        if (path != null && _rateLimits.TryGetValue(path, out var limit))
        {
            var clientKey = GetClientKey(context);
            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers.UserAgent.ToString();

            // Clean up old requests
            CleanupOldRequests(clientKey, limit.Window);

            // Check if rate limit exceeded
            var requests = _requestLog.GetOrAdd(clientKey, _ => new List<DateTime>());
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
                await context.Response.WriteAsync("{\"error\":\"Too many requests. Please try again later.\"}");
                return;
            }

            // Record this request
            requests.Add(DateTime.UtcNow);
        }

        await _next(context);
    }

    private string GetClientKey(HttpContext context)
    {
        // Use IP address as client identifier (in production, consider more sophisticated approaches)
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"{context.Request.Path}_{ipAddress}";
    }

    private void CleanupOldRequests(string clientKey, TimeSpan window)
    {
        if (_requestLog.TryGetValue(clientKey, out var requests))
        {
            var cutoff = DateTime.UtcNow - window;
            requests.RemoveAll(r => r < cutoff);

            // Remove empty lists to prevent memory leaks
            if (requests.Count == 0)
            {
                _requestLog.TryRemove(clientKey, out _);
            }
        }
    }
}


