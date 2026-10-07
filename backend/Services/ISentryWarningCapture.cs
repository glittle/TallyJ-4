namespace Backend.Services;

/// <summary>
/// Sends one warning to Sentry. The ASP.NET Sentry integration records ILogger warnings as
/// breadcrumbs only; its default event level is Error. Abuse alerts call this so the warning
/// is an event even when the process is not configured to promote every warning.
/// </summary>
public interface ISentryWarningCapture
{
    /// <summary>
    /// Records a warning event with structured fields. Does nothing when Sentry is not initialized.
    /// </summary>
    void Capture(string message, IReadOnlyDictionary<string, string> fields);
}
