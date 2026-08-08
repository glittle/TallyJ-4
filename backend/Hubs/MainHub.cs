using System.Security.Claims;
using Backend.Context;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

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
    private readonly MainDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the MainHub.
    /// </summary>
    public MainHub(
        ILogger<MainHub> logger,
        IComputerAssignmentService assignmentService,
        MainDbContext dbContext)
    {
        _logger = logger;
        _assignmentService = assignmentService;
        _dbContext = dbContext;
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
    /// Listen-only multi-election join for known tellers on the dashboard (v3 <c>JoinAll</c> parity).
    /// Joins base <c>Main{guid}</c> and <c>…Known</c> for each election the user is a member of.
    /// Does not assign a computer code (active workstation join remains <see cref="JoinElection"/>).
    /// Guests are rejected. Unauthorized GUIDs are skipped.
    /// </summary>
    /// <param name="electionGuids">Election GUIDs to listen to for status updates.</param>
    /// <returns>GUIDs successfully joined (membership-verified known teller only).</returns>
    public async Task<IReadOnlyList<Guid>> JoinElections(IEnumerable<Guid> electionGuids)
    {
        if (!IsMainTeller(Context.User))
        {
            throw new HubException("Only known tellers can join multiple elections.");
        }

        var userId = GetUserId(Context.User);
        if (userId == null)
        {
            throw new HubException("Authenticated user id is required.");
        }

        var requested = (electionGuids ?? Enumerable.Empty<Guid>())
            .Where(g => g != Guid.Empty)
            .Distinct()
            .ToList();

        if (requested.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        var allowed = await _dbContext.JoinElectionUsers
            .AsNoTracking()
            .Where(jeu => jeu.UserId == userId.Value && requested.Contains(jeu.ElectionGuid))
            .Select(jeu => jeu.ElectionGuid)
            .ToListAsync();

        var allowedSet = allowed.ToHashSet();
        var joined = new List<Guid>(allowedSet.Count);

        foreach (var electionGuid in requested)
        {
            if (!allowedSet.Contains(electionGuid))
            {
                _logger.LogWarning(
                    "Client {ConnectionId} skipped JoinElections for {ElectionGuid}: not a member",
                    Context.ConnectionId,
                    electionGuid);
                continue;
            }

            var groupName = GetGroupName(electionGuid);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName + "Known");
            joined.Add(electionGuid);
        }

        _logger.LogInformation(
            "Client {ConnectionId} joined {JoinedCount} of {RequestedCount} elections for dashboard listen",
            Context.ConnectionId,
            joined.Count,
            requested.Count);

        return joined;
    }

    /// <summary>
    /// Removes the current client from the SignalR group for the specified election.
    /// The client will no longer receive real-time updates about the election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to leave.</param>
    public async Task LeaveElection(Guid electionGuid)
    {
        await RemoveFromElectionGroupsAsync(electionGuid);
        _assignmentService.ReleaseConnection(Context.ConnectionId);
        _logger.LogInformation("Client {ConnectionId} left election {ElectionGuid}",
            Context.ConnectionId, electionGuid);
    }

    /// <summary>
    /// Leaves multiple MainHub election groups without releasing computer assignment
    /// (dashboard multi-listen cleanup; active workstation stays via <see cref="JoinElection"/>).
    /// </summary>
    public async Task LeaveElections(IEnumerable<Guid> electionGuids)
    {
        var toLeave = (electionGuids ?? Enumerable.Empty<Guid>())
            .Where(g => g != Guid.Empty)
            .Distinct()
            .ToList();

        foreach (var electionGuid in toLeave)
        {
            await RemoveFromElectionGroupsAsync(electionGuid);
        }

        if (toLeave.Count > 0)
        {
            _logger.LogInformation(
                "Client {ConnectionId} left {Count} elections for dashboard listen",
                Context.ConnectionId,
                toLeave.Count);
        }
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

    private async Task RemoveFromElectionGroupsAsync(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName + "Known");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName + "Guest");
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

    private static Guid? GetUserId(ClaimsPrincipal? user)
    {
        if (user == null)
        {
            return null;
        }

        var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }

    /// <summary>
    /// Base MainHub group for an election. Clients also join <c>{base}Known</c> or <c>{base}Guest</c>.
    /// Server pushes shared status to the base group via <see cref="ISignalRNotificationService"/>;
    /// role-specific events (e.g. guest <c>electionClosed</c>) use the Known/Guest suffix groups.
    /// </summary>
    public static string GetGroupName(Guid electionGuid) => $"Main{electionGuid}";
}
