using Backend.Helpers;
using Backend.Services;
using Microsoft.Extensions.Logging;

namespace Backend.Tests.UnitTests.Services;

public class BulkSendLogTests
{
    [Fact]
    public void LogChannel_KeepsOnlyTheKnownToken()
    {
        Assert.Equal("email", BulkSendSupport.LogChannel(AnnouncementChannels.Email));
        Assert.Equal("sms", BulkSendSupport.LogChannel("sms"));
        Assert.Equal("whatsapp", BulkSendSupport.LogChannel("whatsapp"));
        Assert.Equal("unknown", BulkSendSupport.LogChannel("email\r\nINJECTED ada@example.com"));
        Assert.Equal("unknown", BulkSendSupport.LogChannel(null));
    }

    [Fact]
    public void LogFailures_WritesTheCountAndChannel_NotAddresses()
    {
        var logger = new ListLogger();
        BulkSendSupport.LogFailures(
            logger,
            Guid.NewGuid(),
            "sms\nINJECTED ada@example.com",
            failedCount: 2);

        var text = string.Join('\n', logger.Messages);
        Assert.Contains("channel unknown", text, StringComparison.Ordinal);
        Assert.Contains("2 failures", text, StringComparison.Ordinal);
        Assert.DoesNotContain("INJECTED", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ada@", text, StringComparison.Ordinal);
        Assert.DoesNotContain("example.com", text, StringComparison.Ordinal);
        Assert.DoesNotContain("@", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\nINJECTED", text, StringComparison.Ordinal);
    }

    [Fact]
    public void LogFailures_KnownEmailChannel_DoesNotCopyAMaskedMailbox()
    {
        var logger = new ListLogger();
        BulkSendSupport.LogFailures(logger, Guid.NewGuid(), AnnouncementChannels.Email, failedCount: 1);

        var text = Assert.Single(logger.Messages);
        Assert.Contains("channel email", text, StringComparison.Ordinal);
        Assert.DoesNotContain("@", text, StringComparison.Ordinal);
        Assert.DoesNotContain("a***", text, StringComparison.Ordinal);
    }

    private sealed class ListLogger : ILogger
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
