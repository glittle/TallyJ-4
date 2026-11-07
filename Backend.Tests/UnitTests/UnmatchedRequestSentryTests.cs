using Backend.Middleware;
using Sentry;

namespace Backend.Tests.UnitTests;

public class UnmatchedRequestSentryTests
{
    [Fact]
    public void Filter_DropsTaggedMessageWithoutAnException()
    {
        var sentryEvent = new SentryEvent();
        sentryEvent.SetTag(UnmatchedRequestSentry.TagName, "true");

        Assert.Null(UnmatchedRequestSentry.Filter(sentryEvent));
    }

    [Fact]
    public void Filter_KeepsTaggedException()
    {
        var sentryEvent = new SentryEvent(new InvalidOperationException("boom"));
        sentryEvent.SetTag(UnmatchedRequestSentry.TagName, "true");

        Assert.Same(sentryEvent, UnmatchedRequestSentry.Filter(sentryEvent));
    }

    [Fact]
    public void Filter_KeepsUntaggedMessage()
    {
        var sentryEvent = new SentryEvent();

        Assert.Same(sentryEvent, UnmatchedRequestSentry.Filter(sentryEvent));
    }

    [Fact]
    public void MarkCurrentScope_DoesNotThrowWhenSentryIsDisabled()
    {
        UnmatchedRequestSentry.MarkCurrentScope();
    }
}
