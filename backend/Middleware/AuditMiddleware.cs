using System.Security.Claims;
using TallyJ4.DTOs.AuditLogs;
using TallyJ4.Services;

namespace TallyJ4.Middleware;

/// <summary>
/// Middleware for logging audit entries for HTTP requests.
/// </summary>
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to process HTTP requests and log audit entries.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="auditLogService">The audit log service for creating audit entries.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
    {
        var shouldLog = ShouldLogRequest(context);
        
        if (shouldLog)
        {
            await _next(context);
            
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                await LogAuditEntry(context, auditLogService);
            }
        }
        else
        {
            await _next(context);
        }
    }

    private bool ShouldLogRequest(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var method = context.Request.Method.ToUpper();
        
        if (path.StartsWith("/api/audit-logs") || 
            path.StartsWith("/swagger") || 
            path.StartsWith("/hubs") ||
            path.StartsWith("/health"))
        {
            return false;
        }
        
        return method is "POST" or "PUT" or "DELETE";
    }

    private async Task LogAuditEntry(HttpContext context, IAuditLogService auditLogService)
    {
        try
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? context.User.FindFirst("sub")?.Value;
            
            var electionGuidClaim = context.User.FindFirst("ElectionGuid")?.Value;
            Guid? electionGuid = electionGuidClaim != null && Guid.TryParse(electionGuidClaim, out var eg) ? eg : null;
            
            if (!electionGuid.HasValue && context.Request.RouteValues.TryGetValue("electionGuid", out var routeElectionGuid))
            {
                if (Guid.TryParse(routeElectionGuid?.ToString(), out var parsedElectionGuid))
                {
                    electionGuid = parsedElectionGuid;
                }
            }
            
            var computerCode = context.Request.Headers["X-Computer-Code"].FirstOrDefault();
            
            var details = $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}";
            
            var hostAndVersion = $"{context.Request.Host} | {context.Request.Headers.UserAgent.FirstOrDefault()}";
            
            var createDto = new CreateAuditLogDto
            {
                ElectionGuid = electionGuid,
                VoterId = userId,
                ComputerCode = computerCode,
                Details = details,
                HostAndVersion = hostAndVersion
            };
            
            await auditLogService.CreateAuditLogAsync(createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log entry");
        }
    }
}
