using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Backend.Domain.Context;
using Backend.Domain.Entities;

namespace Backend.Application.Services.Auth;

/// <summary>
/// Background service that periodically cleans up expired and revoked refresh tokens
/// to prevent database bloat and improve performance.
/// </summary>
public class RefreshTokenCleanupService : BackgroundService
{
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _cleanupInterval;

    public RefreshTokenCleanupService(
        ILogger<RefreshTokenCleanupService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        // Run cleanup daily at 2 AM
        _cleanupInterval = TimeSpan.FromHours(24);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RefreshTokenCleanupService is starting.");

        // Wait until 2 AM to start the first cleanup
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddHours(2);
        if (now > nextRun)
        {
            nextRun = nextRun.AddDays(1);
        }

        var initialDelay = nextRun - now;
        await Task.Delay(initialDelay, stoppingToken);

        using var timer = new PeriodicTimer(_cleanupInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PerformCleanupAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RefreshTokenCleanupService is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in RefreshTokenCleanupService.");
        }
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();

            var cutoffDate = DateTime.UtcNow;

            // Delete expired tokens that haven't been revoked
            var expiredTokens = await dbContext.RefreshTokens
                .Where(rt => rt.ExpiresAt < cutoffDate && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            if (expiredTokens.Any())
            {
                dbContext.RefreshTokens.RemoveRange(expiredTokens);
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaned up {Count} expired refresh tokens.", expiredTokens.Count);
            }
            else
            {
                _logger.LogDebug("No expired refresh tokens found to clean up.");
            }

            // Also clean up old revoked tokens (older than 30 days)
            var oldRevokedTokens = await dbContext.RefreshTokens
                .Where(rt => rt.IsRevoked && rt.CreatedAt < cutoffDate.AddDays(-30))
                .ToListAsync(cancellationToken);

            if (oldRevokedTokens.Any())
            {
                dbContext.RefreshTokens.RemoveRange(oldRevokedTokens);
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaned up {Count} old revoked refresh tokens.", oldRevokedTokens.Count);
            }
            else
            {
                _logger.LogDebug("No old revoked refresh tokens found to clean up.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform refresh token cleanup.");
        }
    }
}

