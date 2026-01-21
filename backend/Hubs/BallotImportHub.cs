using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TallyJ4.Hubs;

[Authorize]
public class BallotImportHub : Hub
{
    private readonly ILogger<BallotImportHub> _logger;

    public BallotImportHub(ILogger<BallotImportHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinImportSession(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined import session for election {ElectionGuid}", 
            Context.ConnectionId, electionGuid);
    }

    public async Task LeaveImportSession(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left import session for election {ElectionGuid}", 
            Context.ConnectionId, electionGuid);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from BallotImportHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private static string GetGroupName(Guid electionGuid) => $"Import_{electionGuid}";
}
