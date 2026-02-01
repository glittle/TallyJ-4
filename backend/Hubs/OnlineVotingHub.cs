using Microsoft.AspNetCore.SignalR;

namespace TallyJ4.Hubs;

public class OnlineVotingHub : Hub
{
    private readonly ILogger<OnlineVotingHub> _logger;

    public OnlineVotingHub(ILogger<OnlineVotingHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinElectionGroup(Guid electionGuid)
    {
        var groupName = $"online-election-{electionGuid}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined online voting group for election {ElectionGuid}", 
            Context.ConnectionId, electionGuid);
    }

    public async Task LeaveElectionGroup(Guid electionGuid)
    {
        var groupName = $"online-election-{electionGuid}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left online voting group for election {ElectionGuid}", 
            Context.ConnectionId, electionGuid);
    }

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

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from OnlineVotingHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
