using Sentry;

namespace Backend.Middleware;

/// <summary>
/// Keeps probe 404s and method mismatches out of Sentry as individual events.
/// Exceptions still pass through.
/// </summary>
public static class UnmatchedRequestSentry
{
    public const string TagName = "unmatched_request";

    /// <summary>
    /// Tags the active Sentry scope when the SDK is enabled. No-op otherwise.
    /// </summary>
    public static void MarkCurrentScope()
    {
        if (!SentrySdk.IsEnabled)
        {
            return;
        }

        SentrySdk.ConfigureScope(static scope => scope.SetTag(TagName, "true"));
    }

    /// <summary>
    /// Drops a non-exception event that was tagged as an unmatched probe.
    /// </summary>
    public static SentryEvent? Filter(SentryEvent sentryEvent)
    {
        ArgumentNullException.ThrowIfNull(sentryEvent);

        if (sentryEvent.Exception is not null)
        {
            return sentryEvent;
        }

        if (sentryEvent.Tags is not null
            && sentryEvent.Tags.TryGetValue(TagName, out var value)
            && string.Equals(value, "true", StringComparison.Ordinal))
        {
            return null;
        }

        return sentryEvent;
    }
}
