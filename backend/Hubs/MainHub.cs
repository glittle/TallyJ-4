using System.Security.Claims;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

/// <summary>
/// Main SignalR hub for election-related real-time communication.
/// Handles client join/leave and Known/Guest group membership.
/// Server-to-client broadcasts (status, guest close-out) go through
/// <see cref="ISignalRNotificationService"/> / <c>IHubContext&lt;MainHub&gt;</c>, not hub instance methods.
/// </summary>
[Authorize]
public class MainHub : Hub
{
    private readonly ILogger<MainHub> _logger;
    private readonly IComputerAssignmentService _assignmentService;

    /// <summary>
    /// Initializes a new instance of the MainHub.
    /// </summary>
    public MainHub(ILogger<MainHub> logger, IComputerAssignmentService assignmentService)
    {
        _logger = logger;
        _assignmentService = assignmentService;
    }

    /// <summary>
    /// Adds the current client to the SignalR group for the specified election and assigns a computer code.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to join.</param>
    /// <param name="clientId">Persistent client identifier for code re-assignment.</param>
    /// <returns>The assigned computer code for this workstation.</returns>
    public async Task<string> JoinElection(Guid electionGuid, string clientId)
    {
        var isMainTeller = IsMainTeller(Context.User);
        if (!isMainTeller && !_assignmentService.CanGuestJoin(electionGuid))
        {
            throw new HubException("No main teller is currently connected to this election.");
        }

        var computerCode = _assignmentService.AssignCode(
            electionGuid,
            clientId,
            Context.ConnectionId,
            isMainTeller);

        var groupName = GetGroupName(electionGuid);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var roleSuffix = isMainTeller ? "Known" : "Guest";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName + roleSuffix);

        _logger.LogInformation(
            "Client {ConnectionId} joined election {ElectionGuid} as {Role} with code {ComputerCode}",
            Context.ConnectionId,
            electionGuid,
            roleSuffix,
            computerCode);

        return computerCode;
    }

    /// <summary>
    /// Removes the current client from the SignalR group for the specified election.
    /// The client will no longer receive real-time updates about the election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to leave.</param>
    public async Task LeaveElection(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName + "Known");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName + "Guest");
        _assignmentService.ReleaseConnection(Context.ConnectionId);
        _logger.LogInformation("Client {ConnectionId} left election {ElectionGuid}",
            Context.ConnectionId, electionGuid);
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// Logs the disconnection event for monitoring purposes.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _assignmentService.ReleaseConnection(Context.ConnectionId);
        _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private static bool IsMainTeller(ClaimsPrincipal? user)
    {
        if (user == null)
        {
            return false;
        }

        var isTellerClaim = user.FindFirst("isTeller")?.Value;
        return !bool.TryParse(isTellerClaim, out var isGuestTeller) || !isGuestTeller;
    }

    /// <summary>
    /// Base MainHub group for an election. Clients also join <c>{base}Known</c> or <c>{base}Guest</c>.
    /// Server pushes shared status to the base group via <see cref="ISignalRNotificationService"/>;
    /// role-specific events (e.g. guest <c>electionClosed</c>) use the Known/Guest suffix groups.
    /// </summary>
    public static string GetGroupName(Guid electionGuid) => $"Main{electionGuid}";
}