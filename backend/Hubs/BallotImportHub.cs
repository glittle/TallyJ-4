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

    // Server-to-client methods for ballot import progress
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

    public async Task ImportError(Guid electionGuid, string errorMessage, int rowNumber)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("importError", errorMessage, rowNumber);

        _logger.LogWarning("Ballot import error for election {ElectionGuid} at row {RowNumber}: {Error}",
            electionGuid, rowNumber, errorMessage);
    }

    public async Task ImportComplete(Guid electionGuid, object summary)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("importComplete", summary);

        _logger.LogInformation("Ballot import completed for election {ElectionGuid}", electionGuid);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from BallotImportHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private static string GetGroupName(Guid electionGuid) => $"BallotImport{electionGuid}";
}
