using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

/// <summary>
/// SignalR hub for real-time communication during election analysis and tally operations.
/// Provides progress updates and status notifications for long-running tally calculations.
/// </summary>
[Authorize]
public class AnalyzeHub : Hub
{
    private readonly ILogger<AnalyzeHub> _logger;

    /// <summary>
    /// Initializes a new instance of the AnalyzeHub.
    /// </summary>
    /// <param name="logger">Logger for recording hub operations and analysis progress.</param>
    public AnalyzeHub(ILogger<AnalyzeHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds the current client to the SignalR group for tally analysis of the specified election.
    /// Clients in this group will receive real-time updates about tally progress and results.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election being analyzed.</param>
    public async Task JoinTallySession(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined tally session for election {ElectionGuid}",
            Context.ConnectionId, electionGuid);
    }

    /// <summary>
    /// Removes the current client from the SignalR group for tally analysis of the specified election.
    /// The client will no longer receive real-time updates about tally progress.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election analysis session to leave.</param>
    public async Task LeaveTallySession(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left tally session for election {ElectionGuid}",
            Context.ConnectionId, electionGuid);
    }

    // Server-to-client methods for tally progress updates
    /// <summary>
    /// Broadcasts a status update message to all clients monitoring the tally analysis.
    /// Used to provide textual feedback about the current analysis state.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election being analyzed.</param>
    /// <param name="message">The status message to broadcast to clients.</param>
    /// <param name="showProgress">Whether to show a progress indicator alongside the message.</param>
    public async Task StatusUpdate(Guid electionGuid, string message, bool showProgress)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("statusUpdate", message, showProgress);

        _logger.LogInformation("Tally status update for election {ElectionGuid}: {Message}", electionGuid, message);
    }

    /// <summary>
    /// Broadcasts detailed progress information about the ongoing tally calculation.
    /// Includes counts of processed ballots and votes along with calculated percentage completion.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election being tallied.</param>
    /// <param name="processedBallots">The number of ballots that have been processed so far.</param>
    /// <param name="totalBallots">The total number of ballots to be processed.</param>
    /// <param name="processedVotes">The number of votes that have been processed so far.</param>
    /// <param name="totalVotes">The total number of votes to be processed.</param>
    public async Task TallyProgress(Guid electionGuid, int processedBallots, int totalBallots, int processedVotes, int totalVotes)
    {
        var groupName = GetGroupName(electionGuid);
        var progress = new
        {
            processedBallots,
            totalBallots,
            processedVotes,
            totalVotes,
            percentage = totalBallots > 0 ? (processedBallots * 100.0 / totalBallots) : 0
        };

        await Clients.Group(groupName).SendAsync("tallyProgress", progress);

        _logger.LogInformation("Tally progress for election {ElectionGuid}: {Processed}/{Total} ballots",
            electionGuid, processedBallots, totalBallots);
    }

    /// <summary>
    /// Broadcasts the final results of the tally calculation to all monitoring clients.
    /// Signals that the analysis process has completed successfully.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election that was tallied.</param>
    /// <param name="results">The complete tally results data to broadcast to clients.</param>
    public async Task TallyComplete(Guid electionGuid, object results)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("tallyComplete", results);

        _logger.LogInformation("Tally completed for election {ElectionGuid}", electionGuid);
    }

    /// <summary>
    /// Called when a client disconnects from the AnalyzeHub.
    /// Logs the disconnection event for monitoring purposes.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from AnalyzeHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private static string GetGroupName(Guid electionGuid) => $"Analyze{electionGuid}";
}



