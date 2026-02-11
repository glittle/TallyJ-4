using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TallyJ4.Application.Services.Auth;
using TallyJ4.Domain.Entities;
using TallyJ4.Domain.Identity;

namespace TallyJ4.Tests.UnitTests;

public class RefreshTokenCleanupServiceTests : ServiceTestBase
{
    private readonly Mock<ILogger<RefreshTokenCleanupService>> _loggerMock;
    private readonly IServiceProvider _serviceProvider;
    private readonly RefreshTokenCleanupService _service;

    public RefreshTokenCleanupServiceTests()
    {
        _loggerMock = new Mock<ILogger<RefreshTokenCleanupService>>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => Context);
        _serviceProvider = serviceCollection.BuildServiceProvider();

        _service = new RefreshTokenCleanupService(_loggerMock.Object, _serviceProvider);
    }

    [Fact]
    public async Task PerformCleanupAsync_WithExpiredTokens_RemovesExpiredTokens()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var expiredToken = new RefreshToken
        {
            UserId = userId,
            Token = "expired-token",
            TokenHash = "hash1",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            IsRevoked = false
        };

        var validToken = new RefreshToken
        {
            UserId = userId,
            Token = "valid-token",
            TokenHash = "hash2",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        Context.RefreshTokens.AddRange(expiredToken, validToken);
        await Context.SaveChangesAsync();

        // Act
        await InvokePerformCleanupAsync();

        // Assert
        var remainingTokens = await Context.RefreshTokens.ToListAsync();
        Assert.Single(remainingTokens);
        Assert.Equal("valid-token", remainingTokens[0].Token);
    }

    [Fact]
    public async Task PerformCleanupAsync_WithOldRevokedTokens_RemovesOldRevokedTokens()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var oldRevokedToken = new RefreshToken
        {
            UserId = userId,
            Token = "old-revoked-token",
            TokenHash = "hash1",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-31), // Older than 30 days
            IsRevoked = true,
            RevokedReason = "User logout"
        };

        var recentRevokedToken = new RefreshToken
        {
            UserId = userId,
            Token = "recent-revoked-token",
            TokenHash = "hash2",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1), // Less than 30 days ago
            IsRevoked = true,
            RevokedReason = "Token refresh"
        };

        Context.RefreshTokens.AddRange(oldRevokedToken, recentRevokedToken);
        await Context.SaveChangesAsync();

        // Act
        await InvokePerformCleanupAsync();

        // Assert
        var remainingTokens = await Context.RefreshTokens.ToListAsync();
        Assert.Single(remainingTokens);
        Assert.Equal("recent-revoked-token", remainingTokens[0].Token);
    }

    [Fact]
    public async Task PerformCleanupAsync_WithMixedTokens_RemovesOnlyExpiredAndOldRevoked()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Expired but not revoked - should be removed
        var expiredToken = new RefreshToken
        {
            UserId = userId,
            Token = "expired-token",
            TokenHash = "hash1",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            IsRevoked = false
        };

        // Valid token - should be kept
        var validToken = new RefreshToken
        {
            UserId = userId,
            Token = "valid-token",
            TokenHash = "hash2",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        // Old revoked token - should be removed
        var oldRevokedToken = new RefreshToken
        {
            UserId = userId,
            Token = "old-revoked-token",
            TokenHash = "hash3",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-31),
            IsRevoked = true,
            RevokedReason = "User logout"
        };

        // Recent revoked token - should be kept
        var recentRevokedToken = new RefreshToken
        {
            UserId = userId,
            Token = "recent-revoked-token",
            TokenHash = "hash4",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = true,
            RevokedReason = "Token refresh"
        };

        Context.RefreshTokens.AddRange(expiredToken, validToken, oldRevokedToken, recentRevokedToken);
        await Context.SaveChangesAsync();

        // Act
        await InvokePerformCleanupAsync();

        // Assert
        var remainingTokens = await Context.RefreshTokens.ToListAsync();
        Assert.Equal(2, remainingTokens.Count);
        Assert.Contains(remainingTokens, t => t.Token == "valid-token");
        Assert.Contains(remainingTokens, t => t.Token == "recent-revoked-token");
        Assert.DoesNotContain(remainingTokens, t => t.Token == "expired-token");
        Assert.DoesNotContain(remainingTokens, t => t.Token == "old-revoked-token");
    }

    [Fact]
    public async Task PerformCleanupAsync_WithNoTokensToClean_DoesNothing()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var validToken = new RefreshToken
        {
            UserId = userId,
            Token = "valid-token",
            TokenHash = "hash1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        Context.RefreshTokens.Add(validToken);
        await Context.SaveChangesAsync();

        // Act
        await InvokePerformCleanupAsync();

        // Assert
        var remainingTokens = await Context.RefreshTokens.ToListAsync();
        Assert.Single(remainingTokens);
        Assert.Equal("valid-token", remainingTokens[0].Token);
    }

    [Fact]
    public async Task PerformCleanupAsync_WithException_LogsError()
    {
        // Arrange
        // Create a scenario that might cause an exception (e.g., by disposing context)
        Context.Dispose();

        // Act
        await InvokePerformCleanupAsync();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    private async Task InvokePerformCleanupAsync()
    {
        // Use reflection to invoke the private PerformCleanupAsync method
        var method = typeof(RefreshTokenCleanupService)
            .GetMethod("PerformCleanupAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);

        var task = (Task)method.Invoke(_service, new object[] { CancellationToken.None });
        await task;
    }
}