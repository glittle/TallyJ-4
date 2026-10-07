using Sentry;

namespace Backend.Services;

/// <summary>
/// Sends abuse warnings through the Sentry SDK.
/// </summary>
public class SentryWarningCapture : ISentryWarningCapture
{
    /// <inheritdoc />
    public void Capture(string message, IReadOnlyDictionary<string, string> fields)
    {
        SentrySdk.CaptureMessage(message, scope =>
        {
            scope.Level = SentryLevel.Warning;
            foreach (var field in fields)
            {
                scope.SetExtra(field.Key, field.Value);
            }
        }, SentryLevel.Warning);
    }
}
