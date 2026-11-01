namespace Backend.Services;

/// <summary>
/// Deletes a rejected owner's elections 30 days after rejection.
/// A super admin can delete them sooner from the review page.
/// </summary>
public class RejectedOwnerCleanupService : BackgroundService
{
    private static readonly TimeSpan FirstDelay = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceProvider _services;
    private readonly ILogger<RejectedOwnerCleanupService> _logger;

    /// <summary>
    /// Initializes the cleanup loop.
    /// </summary>
    public RejectedOwnerCleanupService(
        IServiceProvider services,
        ILogger<RejectedOwnerCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Rejected owner cleanup is starting.");
        await Task.Delay(FirstDelay, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var review = scope.ServiceProvider.GetRequiredService<IOwnerReviewService>();
                var deleted = await review.DeleteExpiredRejectedElectionsAsync(stoppingToken);
                if (deleted > 0)
                {
                    _logger.LogInformation("Deleted {Count} elections for rejected owners", deleted);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Rejected owner cleanup failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
