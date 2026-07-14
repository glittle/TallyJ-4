using Backend;
using Backend.DTOs.Security;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for reading security and operational audit logs.
/// </summary>
[ApiController]
[Route("api/security-audit-logs")]
[Authorize]
public class SecurityAuditLogsController : ControllerBase
{
    private readonly ISecurityAuditService _securityAuditService;
    private readonly ILogger<SecurityAuditLogsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityAuditLogsController"/> class.
    /// </summary>
    public SecurityAuditLogsController(
        ISecurityAuditService securityAuditService,
        ILogger<SecurityAuditLogsController> logger)
    {
        _securityAuditService = securityAuditService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of security audit logs with optional filtering.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<SecurityAuditLogDto>>> GetSecurityAuditLogs(
        [FromQuery] SecurityEventType? eventType = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? onlineVoterId = null,
        [FromQuery] Guid? electionGuid = null,
        [FromQuery] string? email = null,
        [FromQuery] string? ipAddress = null,
        [FromQuery] bool? isSuspicious = null,
        [FromQuery] SecurityEventSeverity? severity = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 200)
        {
            return BadRequest(new
            {
                message = "Invalid pagination parameters. PageNumber must be >= 1, PageSize must be between 1 and 200."
            });
        }

        var filter = new SecurityAuditLogFilterDto
        {
            EventType = eventType,
            UserId = userId,
            OnlineVoterId = onlineVoterId,
            ElectionGuid = electionGuid,
            Email = email,
            IpAddress = ipAddress,
            IsSuspicious = isSuspicious,
            Severity = severity,
            StartDate = startDate,
            EndDate = endDate,
            SearchTerm = searchTerm
        };

        var result = await _securityAuditService.GetSecurityAuditLogsAsync(filter, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific security audit log by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<SecurityAuditLogDto>>> GetSecurityAuditLog(int id)
    {
        var log = await _securityAuditService.GetSecurityAuditLogByIdAsync(id);

        if (log == null)
        {
            return NotFound(ApiResponse<SecurityAuditLogDto>.ErrorResponse("Security audit log not found"));
        }

        return Ok(ApiResponse<SecurityAuditLogDto>.SuccessResponse(log));
    }
}
