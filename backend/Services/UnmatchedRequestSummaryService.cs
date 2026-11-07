using Backend.Middleware;

namespace Backend.Services;

/// <summary>
/// Writes one unmatched-request summary every five minutes, and again on shutdown
/// when the stopping callback has not already drained the counts.
/// </summary>
public sealed class UnmatchedRequestSummaryService : BackgroundService
{
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);

    private readonly UnmatchedRequestTracker _tracker;
    private readonly ILogger<UnmatchedRequestSummaryService> _logger;
    private readonly TimeSpan _interval;

    public UnmatchedRequestSummaryService(
        UnmatchedRequestTracker tracker,
        ILogger<UnmatchedRequestSummaryService> logger)
        : this(tracker, logger, DefaultInterval)
    {
    }

    internal UnmatchedRequestSummaryService(
        UnmatchedRequestTracker tracker,
        ILogger<UnmatchedRequestSummaryService> logger,
        TimeSpan interval)
    {
        _tracker = tracker;
        _logger = logger;
        _interval = interval > TimeSpan.Zero ? interval : DefaultInterval;
    }

    internal TimeSpan Interval => _interval;

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // .NET runs ExecuteAsync on the thread pool. Flush again after that task
            // has finished so a count recorded just before stop is not left behind.
            Flush();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                Flush();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            Flush();
        }
    }

    internal void Flush()
    {
        try
        {
            var summary = _tracker.DrainFormatted();
            if (summary is null)
            {
                return;
            }

            _logger.LogInformation("{UnmatchedRequestSummary}", summary);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to write unmatched request summary");
        }
    }
}
