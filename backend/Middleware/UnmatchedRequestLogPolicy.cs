using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace Backend.Middleware;

/// <summary>
/// Serilog filter for unmatched-route noise.
/// </summary>
public static class UnmatchedRequestLogPolicy
{
    private static IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Points the filter at the current request so warning/error lines emitted while an
    /// unmatched request is finishing can be dropped. The hosting logger runs outside the
    /// pipeline, so the flag lives on <see cref="HttpContext.Items"/> rather than an AsyncLocal.
    /// </summary>
    public static void Bind(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// True when this event should be dropped: the hosting "request was not handled" line,
    /// or a warning/error emitted for a request marked unmatched.
    /// </summary>
    public static bool ShouldSuppress(LogEvent logEvent) =>
        ShouldSuppress(logEvent, _httpContextAccessor?.HttpContext);

    internal static bool ShouldSuppress(LogEvent logEvent, HttpContext? httpContext)
    {
        if (IsHostingRequestUnhandled(logEvent))
        {
            return true;
        }

        if (httpContext is null
            || !httpContext.Items.ContainsKey(UnmatchedRequestClassifier.SuppressLoggingItemKey))
        {
            return false;
        }

        return logEvent.Level >= LogEventLevel.Warning;
    }

    private static bool IsHostingRequestUnhandled(LogEvent logEvent)
    {
        if (logEvent.MessageTemplate.Text.Contains(
                UnmatchedRequestClassifier.UnhandledMessageFragment,
                StringComparison.Ordinal))
        {
            return true;
        }

        if (!TryGetEventId(logEvent, out var eventId)
            || eventId != UnmatchedRequestClassifier.HostingRequestUnhandledEventId)
        {
            return false;
        }

        return IsHostingDiagnostics(logEvent);
    }

    private static bool IsHostingDiagnostics(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("SourceContext", out var source))
        {
            return false;
        }

        var category = source is ScalarValue scalar
            ? scalar.Value as string
            : source.ToString().Trim('"');

        return string.Equals(
            category,
            UnmatchedRequestClassifier.HostingDiagnosticsCategory,
            StringComparison.Ordinal);
    }

    private static bool TryGetEventId(LogEvent logEvent, out int eventId)
    {
        eventId = 0;
        if (!logEvent.Properties.TryGetValue("EventId", out var value))
        {
            return false;
        }

        if (value is StructureValue structure)
        {
            foreach (var property in structure.Properties)
            {
                if (property.Name == "Id"
                    && property.Value is ScalarValue scalar
                    && scalar.Value is int id)
                {
                    eventId = id;
                    return true;
                }
            }
        }

        if (value is ScalarValue direct && direct.Value is int directId)
        {
            eventId = directId;
            return true;
        }

        return false;
    }
}
