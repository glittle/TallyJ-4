using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

/// <summary>
/// SignalR hub for real-time online voting notifications.
/// </summary>
public class OnlineVotingHub : Hub
{
    private readonly ILogger<OnlineVotingHub> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnlineVotingHub"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public OnlineVotingHub(ILogger<OnlineVotingHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds the client to an election-specific SignalR group for real-time updates.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task JoinElectionGroup(Guid electionGuid)
    {
        var groupName = $"online-election-{electionGuid}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined online voting group for election {ElectionGuid}",
            Context.ConnectionId, electionGuid);
    }

    /// <summary>
    /// Removes the client from an election-specific SignalR group.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task LeaveElectionGroup(Guid electionGuid)
    {
        var groupName = $"online-election-{electionGuid}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left online voting group for election {ElectionGuid}",
            Context.ConnectionId, electionGuid);
    }

    /// <summary>
    /// Broadcasts a notification that a ballot has been submitted for an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="totalVotes">The total number of votes submitted.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task BallotSubmitted(Guid electionGuid, int totalVotes)
    {
        var groupName = $"online-election-{electionGuid}";
        await Clients.Group(groupName).SendAsync("OnlineVoteSubmitted", new
        {
            electionGuid,
            totalVotes,
            timestamp = DateTime.UtcNow
        });
        _logger.LogInformation("Ballot submitted notification broadcast for election {ElectionGuid}", electionGuid);
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from OnlineVotingHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}



