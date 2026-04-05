namespace Backend.Services;

/// <summary>
/// Service for sending log messages to remote logging endpoints.
/// </summary>
public interface IRemoteLogService
{
    /// <summary>
    /// Sends a log message to the configured remote logging endpoint.
    /// </summary>
    /// <param name="message">The log message to send.</param>
    /// <param name="level">The log level (e.g., "Error", "Warning", "Info").</param>
    /// <param name="details">Additional details about the log event.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendLogAsync(string message, string level, string? details = null);
}