using System.Security.Claims;
using Backend;
using Backend.DTOs.Security;
using Backend.Services;

namespace Backend.Middleware;

/// <summary>
/// Middleware that records successful mutating API calls into <see cref="ISecurityAuditService"/>.
/// Auth paths that already emit typed security events are skipped to avoid duplicates.
/// </summary>
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISecurityAuditService securityAuditService)
    {
        var shouldLog = ShouldLogRequest(context);

        if (shouldLog)
        {
            await _next(context);

            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                await LogAuditEntry(context, securityAuditService);
            }
        }
        else
        {
            await _next(context);
        }
    }

    private static bool ShouldLogRequest(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var method = context.Request.Method.ToUpperInvariant();

        // Skip infra, hubs, and routes that already write typed SecurityAuditLogs.
        if (path.StartsWith("/api/auth") ||
            path.StartsWith("/api/account") ||
            path.StartsWith("/api/audit-logs") ||
            path.StartsWith("/api/security-audit-logs") ||
            path.StartsWith("/swagger") ||
            path.StartsWith("/hubs") ||
            path.StartsWith("/health"))
        {
            return false;
        }

        return method is "POST" or "PUT" or "DELETE" or "PATCH";
    }

    private async Task LogAuditEntry(HttpContext context, ISecurityAuditService securityAuditService)
    {
        try
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? context.User.FindFirst("sub")?.Value;

            Guid? electionGuid = null;
            var electionGuidClaim = context.User.FindFirst("ElectionGuid")?.Value;
            if (electionGuidClaim != null && Guid.TryParse(electionGuidClaim, out var eg))
            {
                electionGuid = eg;
            }

            if (!electionGuid.HasValue &&
                context.Request.RouteValues.TryGetValue("electionGuid", out var routeElectionGuid) &&
                Guid.TryParse(routeElectionGuid?.ToString(), out var parsedElectionGuid))
            {
                electionGuid = parsedElectionGuid;
            }

            var computerCode = context.Request.Headers["X-Computer-Code"].FirstOrDefault();
            var metadata = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(computerCode))
            {
                metadata["computerCode"] = computerCode;
            }

            if (context.Request.RouteValues.TryGetValue("locationGuid", out var routeLocationGuid) &&
                Guid.TryParse(routeLocationGuid?.ToString(), out var locationGuid))
            {
                metadata["locationGuid"] = locationGuid.ToString();
            }

            var details = $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}";
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();

            await securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.OperationalActivity,
                UserId = userId,
                ElectionGuid = electionGuid,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Details = details,
                Severity = SecurityEventSeverity.Info,
                Metadata = metadata.Count > 0 ? metadata : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create operational audit log entry");
        }
    }
}
