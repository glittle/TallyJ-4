using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Backend.Domain;
using Backend.Services;
using Xunit;

namespace Backend.Tests.UnitTests;

public class SecurityAuditServiceTests : ServiceTestBase
{
    private readonly Mock<ILogger<SecurityAuditService>> _loggerMock;
    private readonly SecurityAuditService _service;

    public SecurityAuditServiceTests()
    {
        _loggerMock = new Mock<ILogger<SecurityAuditService>>();
        _service = new SecurityAuditService(Context, Mapper, _loggerMock.Object);
    }

    [Fact]
    public async Task LogSecurityEventAsync_ShouldCreateSecurityAuditLog()
    {
        // Arrange
        var createDto = new Backend.DTOs.Security.CreateSecurityAuditLogDto
        {
            EventType = Backend.Domain.SecurityEventType.LoginSuccess,
            UserId = "user123",
            Email = "test@example.com",
            IpAddress = "192.168.1.1",
            UserAgent = "Test Browser",
            Details = "Test login",
            IsSuspicious = false,
            Severity = Backend.Domain.SecurityEventSeverity.Info
        };

        // Act
        await _service.LogSecurityEventAsync(createDto);

        // Assert
        var log = await Context.SecurityAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(SecurityEventType.LoginSuccess, log.EventType);
        Assert.Equal("user123", log.UserId);
        Assert.Equal("test@example.com", log.Email);
        Assert.Equal("192.168.1.1", log.IpAddress);
        Assert.Equal("Test Browser", log.UserAgent);
        Assert.Equal("Test login", log.Details);
        Assert.False(log.IsSuspicious);
        Assert.Equal(SecurityEventSeverity.Info, log.Severity);
    }

    [Fact]
    public async Task LogSecurityEventAsync_ShouldHandleSuspiciousEvents()
    {
        // Arrange
        var createDto = new Backend.DTOs.Security.CreateSecurityAuditLogDto
        {
            EventType = Backend.Domain.SecurityEventType.LoginFailure,
            Email = "test@example.com",
            IpAddress = "192.168.1.1",
            Details = "Invalid credentials",
            IsSuspicious = true,
            Severity = Backend.Domain.SecurityEventSeverity.Warning
        };

        // Act
        await _service.LogSecurityEventAsync(createDto);

        // Assert
        var log = await Context.SecurityAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.True(log.IsSuspicious);
        Assert.Equal(SecurityEventSeverity.Warning, log.Severity);
    }

    [Fact]
    public async Task GetSecurityAuditLogsAsync_ShouldReturnPagedResults()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await _service.LogSecurityEventAsync(new Backend.DTOs.Security.CreateSecurityAuditLogDto
            {
                EventType = Backend.Domain.SecurityEventType.LoginSuccess,
                Details = $"Login {i}",
                IsSuspicious = false,
                Severity = Backend.Domain.SecurityEventSeverity.Info
            });
        }

        // Act
        var result = await _service.GetSecurityAuditLogsAsync(pageNumber: 1, pageSize: 3);

        // Assert
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task GetSecurityAuditLogsAsync_ShouldFilterByEventType()
    {
        // Arrange
        await _service.LogSecurityEventAsync(new Backend.DTOs.Security.CreateSecurityAuditLogDto
        {
            EventType = Backend.Domain.SecurityEventType.LoginSuccess,
            Details = "Success login",
            IsSuspicious = false,
            Severity = Backend.Domain.SecurityEventSeverity.Info
        });

        await _service.LogSecurityEventAsync(new Backend.DTOs.Security.CreateSecurityAuditLogDto
        {
            EventType = Backend.Domain.SecurityEventType.LoginFailure,
            Details = "Failed login",
            IsSuspicious = false,
            Severity = Backend.Domain.SecurityEventSeverity.Info
        });

        // Act
        var result = await _service.GetSecurityAuditLogsAsync(
            filter: new SecurityAuditLogFilterDto { EventType = Backend.Domain.SecurityEventType.LoginFailure });

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(Backend.Domain.SecurityEventType.LoginFailure, result.Items[0].EventType);
    }

    [Fact]
    public async Task GetSecurityStatisticsAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        await _service.LogSecurityEventAsync(new Backend.DTOs.Security.CreateSecurityAuditLogDto
        {
            EventType = Backend.Domain.SecurityEventType.LoginSuccess,
            IsSuspicious = false,
            Severity = Backend.Domain.SecurityEventSeverity.Info
        });

        await _service.LogSecurityEventAsync(new Backend.DTOs.Security.CreateSecurityAuditLogDto
        {
            EventType = Backend.Domain.SecurityEventType.LoginFailure,
            IsSuspicious = true,
            Severity = Backend.Domain.SecurityEventSeverity.Warning
        });

        await _service.LogSecurityEventAsync(new Backend.DTOs.Security.CreateSecurityAuditLogDto
        {
            EventType = Backend.Domain.SecurityEventType.RateLimitExceeded,
            IsSuspicious = true,
            Severity = Backend.Domain.SecurityEventSeverity.Warning
        });

        // Act
        var stats = await _service.GetSecurityStatisticsAsync(hours: 1);

        // Assert
        Assert.Equal(3, stats.TotalEvents);
        Assert.Equal(2, stats.SuspiciousEvents);
        Assert.Equal(1, stats.SuccessfulLogins);
        Assert.Equal(1, stats.FailedLoginAttempts);
        Assert.Equal(1, stats.RateLimitViolations);
    }

    [Fact]
    public async Task DetectSuspiciousPatternsAsync_ShouldDetectBruteForce()
    {
        // Arrange - Create multiple failed login attempts from same IP
        var ipAddress = "192.168.1.100";
        for (int i = 0; i < 6; i++)
        {
            await _service.LogSecurityEventAsync(new Backend.DTOs.Security.CreateSecurityAuditLogDto
            {
                EventType = Backend.Domain.SecurityEventType.LoginFailure,
                IpAddress = ipAddress,
                Email = $"user{i}@example.com",
                IsSuspicious = false,
                Severity = Backend.Domain.SecurityEventSeverity.Info
            });
        }

        // Act - Trigger pattern detection with another failure
        await _service.LogSecurityEventAsync(new Backend.DTOs.Security.CreateSecurityAuditLogDto
        {
            EventType = Backend.Domain.SecurityEventType.LoginFailure,
            IpAddress = ipAddress,
            Email = "test@example.com",
            IsSuspicious = false,
            Severity = Backend.Domain.SecurityEventSeverity.Info
        });

        // Assert - Should have detected brute force attack
        var bruteForceLogs = await Context.SecurityAuditLogs
            .Where(l => l.EventType == Backend.Domain.SecurityEventType.BruteForceAttemptDetected)
            .ToListAsync();

        Assert.NotEmpty(bruteForceLogs);
        var bruteForceLog = bruteForceLogs.First();
        Assert.Equal(ipAddress, bruteForceLog.IpAddress);
        Assert.True(bruteForceLog.IsSuspicious);
        Assert.Equal(Backend.Domain.SecurityEventSeverity.Critical, bruteForceLog.Severity);
    }
}



