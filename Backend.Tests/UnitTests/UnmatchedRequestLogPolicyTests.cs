using System.Globalization;
using Backend.Middleware;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace Backend.Tests.UnitTests;

public class UnmatchedRequestLogPolicyTests
{
    [Fact]
    public void ShouldSuppress_DropsUnhandledPipelineMessage()
    {
        var logEvent = Parse(
            LogEventLevel.Information,
            "Request reached the end of the middleware pipeline without being handled by application code. Request path: GET http://localhost/account/billing, Response status code: 404");

        Assert.True(UnmatchedRequestLogPolicy.ShouldSuppress(logEvent));
    }

    [Fact]
    public void ShouldSuppress_DropsHostingEventId16()
    {
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            exception: null,
            new MessageTemplateParser().Parse("Request finished"),
            [
                new LogEventProperty(
                    "SourceContext",
                    new ScalarValue(UnmatchedRequestClassifier.HostingDiagnosticsCategory)),
                new LogEventProperty(
                    "EventId",
                    new StructureValue(
                    [
                        new LogEventProperty("Id", new ScalarValue(UnmatchedRequestClassifier.HostingRequestUnhandledEventId)),
                        new LogEventProperty("Name", new ScalarValue("RequestUnhandled"))
                    ]))
            ]);

        Assert.True(UnmatchedRequestLogPolicy.ShouldSuppress(logEvent));
    }

    [Fact]
    public void ShouldSuppress_KeepsSameEventIdFromAnotherCategory()
    {
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Warning,
            exception: null,
            new MessageTemplateParser().Parse("Election not found"),
            [
                new LogEventProperty("SourceContext", new ScalarValue("Backend.Services.ElectionService")),
                new LogEventProperty(
                    "EventId",
                    new StructureValue([new LogEventProperty("Id", new ScalarValue(16))]))
            ]);

        Assert.False(UnmatchedRequestLogPolicy.ShouldSuppress(logEvent));
    }

    [Fact]
    public void RequestMarkedUnmatched_DropsWarningAndErrorButKeepsInformation()
    {
        var context = new DefaultHttpContext();
        context.Items[UnmatchedRequestClassifier.SuppressLoggingItemKey] = true;

        Assert.True(UnmatchedRequestLogPolicy.ShouldSuppress(Parse(LogEventLevel.Warning, "probe warning"), context));
        Assert.True(UnmatchedRequestLogPolicy.ShouldSuppress(Parse(LogEventLevel.Error, "probe error"), context));
        Assert.False(UnmatchedRequestLogPolicy.ShouldSuppress(Parse(LogEventLevel.Information, "Request finished"), context));
        Assert.False(UnmatchedRequestLogPolicy.ShouldSuppress(Parse(LogEventLevel.Error, "real failure"), httpContext: null));
    }

    [Fact]
    public void SerilogFilter_DropsProbeLineAndKeepsApplicationWarning()
    {
        var sink = new ListSink();
        using var logger = new LoggerConfiguration()
            .Filter.ByExcluding(UnmatchedRequestLogPolicy.ShouldSuppress)
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information(
            "Request reached the end of the middleware pipeline without being handled by application code. Request path: GET http://localhost/index, Response status code: 404");
        logger.Warning("Election not found");
        logger.Error("real failure");

        Assert.Equal(["Election not found", "real failure"], sink.Messages);
    }

    private static LogEvent Parse(LogEventLevel level, string message)
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            level,
            exception: null,
            new MessageTemplateParser().Parse(message),
            []);
    }

    private sealed class ListSink : ILogEventSink
    {
        public List<string> Messages { get; } = [];

        public void Emit(LogEvent logEvent)
        {
            Messages.Add(logEvent.RenderMessage(CultureInfo.InvariantCulture));
        }
    }
}
