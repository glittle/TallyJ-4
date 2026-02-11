using TallyJ4.DTOs.Security;
using TallyJ4.Models;

namespace TallyJ4.Services;

/// <summary>
/// Service for managing security audit logs.
/// </summary>
public interface ISecurityAuditService
{
    /// <summary>
    /// Logs a security event asynchronously.
    /// </summary>
    /// <param name="createDto">The security audit log creation data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogSecurityEventAsync(CreateSecurityAuditLogDto createDto);

    /// <summary>
    /// Retrieves a paginated list of security audit logs with optional filtering.
    /// </summary>
    /// <param name="filter">Optional filter criteria for security audit logs.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paginated response containing security audit log DTOs.</returns>
    Task<PaginatedResponse<SecurityAuditLogDto>> GetSecurityAuditLogsAsync(
        SecurityAuditLogFilterDto? filter = null,
        int pageNumber = 1,
        int pageSize = 50);

    /// <summary>
    /// Retrieves a security audit log entry by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the security audit log.</param>
    /// <returns>The security audit log DTO if found, otherwise null.</returns>
    Task<SecurityAuditLogDto?> GetSecurityAuditLogByIdAsync(int id);

    /// <summary>
    /// Gets security statistics for monitoring and alerting.
    /// </summary>
    /// <param name="hours">Number of hours to look back for statistics.</param>
    /// <returns>Security statistics including suspicious events and patterns.</returns>
    Task<SecurityStatisticsDto> GetSecurityStatisticsAsync(int hours = 24);
}

/// <summary>
/// Filter criteria for security audit logs.
/// </summary>
public class SecurityAuditLogFilterDto
{
    /// <summary>
    /// Filter by specific event type.
    /// </summary>
    public TallyJ4.Domain.SecurityEventType? EventType { get; set; }

    /// <summary>
    /// Filter by user ID.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Filter by email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Filter by IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Filter by suspicious events only.
    /// </summary>
    public bool? IsSuspicious { get; set; }

    /// <summary>
    /// Filter by severity level.
    /// </summary>
    public TallyJ4.Domain.SecurityEventSeverity? Severity { get; set; }

    /// <summary>
    /// Start date for filtering.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for filtering.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Search term for details field.
    /// </summary>
    public string? SearchTerm { get; set; }
}

/// <summary>
/// Security statistics for monitoring and alerting.
/// </summary>
public class SecurityStatisticsDto
{
    /// <summary>
    /// Total number of security events in the time period.
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Number of suspicious events.
    /// </summary>
    public int SuspiciousEvents { get; set; }

    /// <summary>
    /// Number of failed login attempts.
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// Number of successful logins.
    /// </summary>
    public int SuccessfulLogins { get; set; }

    /// <summary>
    /// Number of account lockouts.
    /// </summary>
    public int AccountLockouts { get; set; }

    /// <summary>
    /// Number of rate limit violations.
    /// </summary>
    public int RateLimitViolations { get; set; }

    /// <summary>
    /// Top IP addresses by suspicious activity.
    /// </summary>
    public Dictionary<string, int> TopSuspiciousIPs { get; set; } = new();

    /// <summary>
    /// Events grouped by type.
    /// </summary>
    public Dictionary<TallyJ4.Domain.SecurityEventType, int> EventsByType { get; set; } = new();
}