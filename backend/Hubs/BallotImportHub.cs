using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

/// <summary>
/// SignalR hub for real-time communication during ballot import operations.
/// Provides progress updates, error notifications, and completion status for bulk ballot imports.
/// </summary>
[Authorize]
public class BallotImportHub : Hub
{
    private readonly ILogger<BallotImportHub> _logger;

    /// <summary>
    /// Initializes a new instance of the BallotImportHub.
    /// </summary>
    /// <param name="logger">Logger for recording hub operations and import progress.</param>
    public BallotImportHub(ILogger<BallotImportHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds the current client to the SignalR group for ballot import monitoring of the specified election.
    /// Clients in this group will receive real-time updates about import progress and status.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election where ballots are being imported.</param>
    public async Task JoinImportSession(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined import session for election {ElectionGuid}",
            Context.ConnectionId, electionGuid);
    }

    /// <summary>
    /// Removes the current client from the SignalR group for ballot import monitoring of the specified election.
    /// The client will no longer receive real-time updates about import progress.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election import session to leave.</param>
    public async Task LeaveImportSession(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left import session for election {ElectionGuid}",
            Context.ConnectionId, electionGuid);
    }

    // Server-to-client methods for ballot import progress
    /// <summary>
    /// Broadcasts progress information about the ongoing ballot import operation.
    /// Includes counts of processed rows and calculated percentage completion.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election where ballots are being imported.</param>
    /// <param name="processedRows">The number of rows that have been processed so far.</param>
    /// <param name="totalRows">The total number of rows to be imported.</param>
    /// <param name="status">A descriptive status message about the current import phase.</param>
    public async Task ImportProgress(Guid electionGuid, int processedRows, int totalRows, string status)
    {
        var groupName = GetGroupName(electionGuid);
        var progress = new
        {
            processedRows,
            totalRows,
            status,
            percentage = totalRows > 0 ? (processedRows * 100.0 / totalRows) : 0
        };

        await Clients.Group(groupName).SendAsync("importProgress", progress);

        _logger.LogInformation("Ballot import progress for election {ElectionGuid}: {Processed}/{Total} rows - {Status}",
            electionGuid, processedRows, totalRows, status);
    }

    /// <summary>
    /// Broadcasts an error that occurred during ballot import to all monitoring clients.
    /// Includes the error message and the row number where the error occurred.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election where the import error occurred.</param>
    /// <param name="errorMessage">A description of the error that occurred during import.</param>
    /// <param name="rowNumber">The row number in the import file where the error occurred.</param>
    public async Task ImportError(Guid electionGuid, string errorMessage, int rowNumber)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("importError", errorMessage, rowNumber);

        _logger.LogWarning("Ballot import error for election {ElectionGuid} at row {RowNumber}: {Error}",
            electionGuid, rowNumber, errorMessage);
    }

    /// <summary>
    /// Broadcasts the completion of the ballot import operation to all monitoring clients.
    /// Includes a summary of the import results and statistics.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election where the import was completed.</param>
    /// <param name="summary">A summary object containing import statistics and results.</param>
    public async Task ImportComplete(Guid electionGuid, object summary)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("importComplete", summary);

        _logger.LogInformation("Ballot import completed for election {ElectionGuid}", electionGuid);
    }

    /// <summary>
    /// Called when a client disconnects from the BallotImportHub.
    /// Logs the disconnection event for monitoring purposes.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from BallotImportHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private static string GetGroupName(Guid electionGuid) => $"BallotImport{electionGuid}";
}



