using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

/// <summary>
/// SignalR hub for real-time communication during ballot import operations.
/// Clients join/leave import sessions; server pushes progress via
/// <see cref="Backend.Services.ISignalRNotificationService"/> / <c>IHubContext&lt;BallotImportHub&gt;</c>.
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

    /// <summary>
    /// Group name for ballot import progress for an election.
    /// </summary>
    public static string GetGroupName(Guid electionGuid) => $"BallotImport{electionGuid}";
}
