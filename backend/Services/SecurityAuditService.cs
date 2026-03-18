using Mapster;
using Microsoft.EntityFrameworkCore;
using Backend.Domain;
using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.DTOs.Security;
using Backend.Models;
using MapsterMapper;

namespace Backend.Services;

/// <summary>
/// Service implementation for managing security audit logs.
/// </summary>
public class SecurityAuditService : ISecurityAuditService
{
    private readonly MainDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<SecurityAuditService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityAuditService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="mapper">The Mapster instance.</param>
    /// <param name="logger">The logger instance.</param>
    public SecurityAuditService(MainDbContext context, IMapper mapper, ILogger<SecurityAuditService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogSecurityEventAsync(CreateSecurityAuditLogDto createDto)
    {
        try
        {
            var securityLog = new SecurityAuditLog
            {
                Timestamp = DateTime.UtcNow,
                EventType = createDto.EventType,
                UserId = createDto.UserId,
                Email = createDto.Email,
                IpAddress = createDto.IpAddress,
                UserAgent = createDto.UserAgent,
                Details = createDto.Details,
                IsSuspicious = createDto.IsSuspicious,
                Severity = createDto.Severity,
                MetadataJson = createDto.Metadata != null ? System.Text.Json.JsonSerializer.Serialize(createDto.Metadata) : null
            };

            _context.SecurityAuditLogs.Add(securityLog);
            await _context.SaveChangesAsync();

            // Log to structured logging as well for immediate visibility
            var logLevel = createDto.Severity switch
            {
                SecurityEventSeverity.Debug => LogLevel.Debug,
                SecurityEventSeverity.Info => LogLevel.Information,
                SecurityEventSeverity.Warning => LogLevel.Warning,
                SecurityEventSeverity.Error => LogLevel.Error,
                SecurityEventSeverity.Critical => LogLevel.Critical,
                _ => LogLevel.Information
            };

            var message = $"Security Event: {createDto.EventType} - User: {createDto.UserId ?? "N/A"} - IP: {createDto.IpAddress ?? "N/A"} - Suspicious: {createDto.IsSuspicious}";
            if (!string.IsNullOrEmpty(createDto.Details))
            {
                message += $" - Details: {createDto.Details}";
            }

            _logger.Log(logLevel, message);

            // Additional alerting for critical security events
            if (createDto.Severity >= SecurityEventSeverity.Warning || createDto.IsSuspicious)
            {
                _logger.LogWarning("SECURITY ALERT: {EventType} detected for user {UserId} from IP {IpAddress}",
                    createDto.EventType, createDto.UserId ?? "unknown", createDto.IpAddress ?? "unknown");
            }

            // Check for suspicious patterns
            await DetectSuspiciousPatternsAsync(createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event: {EventType}", createDto.EventType);
            // Don't throw - security logging failures shouldn't break the main flow
        }
    }

    /// <inheritdoc />
    public async Task<PaginatedResponse<SecurityAuditLogDto>> GetSecurityAuditLogsAsync(
        SecurityAuditLogFilterDto? filter = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        var query = _context.SecurityAuditLogs.AsQueryable();

        if (filter != null)
        {
            if (filter.EventType.HasValue)
            {
                query = query.Where(l => l.EventType == filter.EventType.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.UserId))
            {
                query = query.Where(l => l.UserId == filter.UserId);
            }

            if (!string.IsNullOrWhiteSpace(filter.Email))
            {
                query = query.Where(l => l.Email!.ToLower().Contains(filter.Email.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(filter.IpAddress))
            {
                query = query.Where(l => l.IpAddress == filter.IpAddress);
            }

            if (filter.IsSuspicious.HasValue)
            {
                query = query.Where(l => l.IsSuspicious == filter.IsSuspicious.Value);
            }

            if (filter.Severity.HasValue)
            {
                query = query.Where(l => l.Severity == filter.Severity.Value);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(l => l.Timestamp >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(l => l.Timestamp <= filter.EndDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(l =>
                    (l.Details != null && l.Details.ToLower().Contains(searchTerm)) ||
                    (l.UserAgent != null && l.UserAgent.ToLower().Contains(searchTerm)));
            }
        }

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var logDtos = logs.Select(log => new SecurityAuditLogDto
        {
            Id = log.Id,
            Timestamp = log.Timestamp,
            EventType = log.EventType,
            UserId = log.UserId,
            Email = log.Email,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            Details = log.Details,
            IsSuspicious = log.IsSuspicious,
            Severity = log.Severity,
            Metadata = !string.IsNullOrEmpty(log.MetadataJson)
                ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(log.MetadataJson)
                : null
        }).ToList();

        _logger.LogInformation(
            "Retrieved {Count} security audit logs (page {PageNumber} of {TotalPages})",
            logDtos.Count,
            pageNumber,
            (totalCount + pageSize - 1) / pageSize);

        return PaginatedResponse<SecurityAuditLogDto>.Create(logDtos, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<SecurityAuditLogDto?> GetSecurityAuditLogByIdAsync(int id)
    {
        var log = await _context.SecurityAuditLogs
            .Where(l => l.Id == id)
            .FirstOrDefaultAsync();

        if (log == null)
        {
            _logger.LogWarning("Security audit log with Id {Id} not found", id);
            return null;
        }

        var logDto = new SecurityAuditLogDto
        {
            Id = log.Id,
            Timestamp = log.Timestamp,
            EventType = log.EventType,
            UserId = log.UserId,
            Email = log.Email,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            Details = log.Details,
            IsSuspicious = log.IsSuspicious,
            Severity = log.Severity,
            Metadata = !string.IsNullOrEmpty(log.MetadataJson)
                ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(log.MetadataJson)
                : null
        };

        _logger.LogInformation("Retrieved security audit log {Id}", id);

        return logDto;
    }

    /// <inheritdoc />
    public async Task<SecurityStatisticsDto> GetSecurityStatisticsAsync(int hours = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);

        var logs = await _context.SecurityAuditLogs
            .Where(l => l.Timestamp >= cutoffTime)
            .ToListAsync();

        var stats = new SecurityStatisticsDto
        {
            TotalEvents = logs.Count,
            SuspiciousEvents = logs.Count(l => l.IsSuspicious),
            FailedLoginAttempts = logs.Count(l => l.EventType == Backend.Domain.SecurityEventType.LoginFailure),
            SuccessfulLogins = logs.Count(l => l.EventType == Backend.Domain.SecurityEventType.LoginSuccess),
            AccountLockouts = logs.Count(l => l.EventType == Backend.Domain.SecurityEventType.AccountLocked),
            RateLimitViolations = logs.Count(l => l.EventType == Backend.Domain.SecurityEventType.RateLimitExceeded)
        };

        // Top suspicious IPs
        stats.TopSuspiciousIPs = logs
            .Where(l => l.IsSuspicious && !string.IsNullOrEmpty(l.IpAddress))
            .GroupBy(l => l.IpAddress!)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());

        // Events by type
        stats.EventsByType = logs
            .GroupBy(l => l.EventType)
            .ToDictionary(g => g.Key, g => g.Count());

        _logger.LogInformation(
            "Generated security statistics for last {Hours} hours: {TotalEvents} events, {SuspiciousEvents} suspicious",
            hours, stats.TotalEvents, stats.SuspiciousEvents);

        return stats;
    }

    /// <summary>
    /// Detects and logs suspicious patterns in security events.
    /// </summary>
    /// <param name="createDto">The security audit log entry to analyze.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DetectSuspiciousPatternsAsync(DTOs.Security.CreateSecurityAuditLogDto createDto)
    {
        // Check for brute force patterns
        if (createDto.EventType == Backend.Domain.SecurityEventType.LoginFailure && !string.IsNullOrEmpty(createDto.IpAddress))
        {
            await CheckForBruteForceAttackAsync(createDto.IpAddress, createDto.Email);
        }

        // Check for unusual login locations (simplified - in production would use geo-IP)
        if (createDto.EventType == Backend.Domain.SecurityEventType.LoginSuccess && !string.IsNullOrEmpty(createDto.IpAddress))
        {
            await CheckForUnusualLoginLocationAsync(createDto.UserId, createDto.IpAddress);
        }
    }

    private async Task CheckForBruteForceAttackAsync(string ipAddress, string? email)
    {
        var recentFailures = await _context.SecurityAuditLogs
            .Where(l => l.IpAddress == ipAddress &&
                       l.EventType == Backend.Domain.SecurityEventType.LoginFailure &&
                       l.Timestamp >= DateTime.UtcNow.AddMinutes(-15))
            .CountAsync();

        if (recentFailures >= 5)
        {
            await LogSecurityEventAsync(new DTOs.Security.CreateSecurityAuditLogDto
            {
                EventType = Backend.Domain.SecurityEventType.BruteForceAttemptDetected,
                IpAddress = ipAddress,
                Email = email,
                Details = $"Brute force attack detected: {recentFailures} failed login attempts from {ipAddress} in the last 15 minutes",
                IsSuspicious = true,
                Severity = Backend.Domain.SecurityEventSeverity.Critical
            });
        }
    }

    private async Task CheckForUnusualLoginLocationAsync(string? userId, string ipAddress)
    {
        if (string.IsNullOrEmpty(userId)) return;

        // Get user's recent successful logins from different IPs
        var recentLogins = await _context.SecurityAuditLogs
            .Where(l => l.UserId == userId &&
                       l.EventType == Backend.Domain.SecurityEventType.LoginSuccess &&
                       l.Timestamp >= DateTime.UtcNow.AddDays(-30) &&
                       l.IpAddress != ipAddress)
            .Select(l => l.IpAddress)
            .Distinct()
            .ToListAsync();

        // If user has logged in from many different IPs recently, flag as suspicious
        if (recentLogins.Count >= 3)
        {
            await LogSecurityEventAsync(new DTOs.Security.CreateSecurityAuditLogDto
            {
                EventType = Backend.Domain.SecurityEventType.UnusualLoginLocation,
                UserId = userId,
                IpAddress = ipAddress,
                Details = $"Unusual login location detected: user logged in from {ipAddress} after logging from {recentLogins.Count} different IPs in the last 30 days",
                IsSuspicious = true,
                Severity = Backend.Domain.SecurityEventSeverity.Warning
            });
        }
    }
}



