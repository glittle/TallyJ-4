namespace Backend.Services;

/// <summary>
/// Service for sending log messages to remote logging endpoints.
/// </summary>
public interface IRemoteLogService
{
    /// <summary>
    /// Sends a log message to the configured remote logging endpoint.
    /// </summary>
    /// <param name="message">The message for the log.</param>
    /// <param name="userName">The username of the admin or voter or teller computer.</param>
    /// <param name="electionName">Election name</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SendLogAsync(string message, string? userName = null, string? electionName = null);
}