using Backend.Middleware;
using Backend.Services;
using Microsoft.Extensions.Logging;

namespace Backend.Tests.UnitTests;

public class UnmatchedRequestSummaryServiceTests
{
    [Fact]
    public void DefaultInterval_IsFiveMinutes()
    {
        Assert.Equal(TimeSpan.FromMinutes(5), UnmatchedRequestSummaryService.DefaultInterval);
    }

    [Fact]
    public void Flush_WritesOneLineAndSkipsAnEmptyWindow()
    {
        var tracker = new UnmatchedRequestTracker();
        var logger = new CollectingLogger<UnmatchedRequestSummaryService>();
        var service = new UnmatchedRequestSummaryService(tracker, logger, TimeSpan.FromHours(1));

        Assert.Equal(TimeSpan.FromHours(1), service.Interval);

        tracker.Record("/index");
        tracker.Record("/INDEX?x=1");
        tracker.Record("/account/billing");
        service.Flush();
        service.Flush();

        Assert.Equal(
            ["Unmatched request summary (last 5 min): /index (2 calls), /account/billing (1 call)"],
            logger.Messages);
    }

    [Fact]
    public async Task StopAsync_FlushesPendingCounts()
    {
        var tracker = new UnmatchedRequestTracker();
        var logger = new CollectingLogger<UnmatchedRequestSummaryService>();
        var service = new UnmatchedRequestSummaryService(tracker, logger, TimeSpan.FromHours(1));

        await service.StartAsync(CancellationToken.None);
        tracker.Record("/account/settings");
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(
            ["Unmatched request summary (last 5 min): /account/settings (1 call)"],
            logger.Messages);
    }

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
