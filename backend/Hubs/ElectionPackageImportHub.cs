using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

/// <summary>
/// SignalR hub for election package load progress on the dashboard (v3 ImportHub <c>loaderStatus</c>).
/// Groups are <strong>user-scoped</strong> (not election-scoped): the election does not exist yet
/// during package import, and concurrent known tellers must not share streams.
/// Server pushes via <see cref="Backend.Services.ISignalRNotificationService"/> /
/// <c>IHubContext&lt;ElectionPackageImportHub&gt;</c> only — join/leave here.
/// </summary>
[Authorize]
public class ElectionPackageImportHub : Hub
{
    private readonly ILogger<ElectionPackageImportHub> _logger;

    /// <summary>
    /// Initializes a new instance of the ElectionPackageImportHub.
    /// </summary>
    public ElectionPackageImportHub(ILogger<ElectionPackageImportHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Joins the caller to their personal package-import progress group (known tellers only).
    /// </summary>
    public async Task JoinSession()
    {
        if (!IsKnownTeller(Context.User))
        {
            throw new HubException("Only known tellers can join election package import progress.");
        }

        var userId = GetUserId(Context.User);
        if (userId == null)
        {
            throw new HubException("Authenticated user id is required.");
        }

        var groupName = GetGroupName(userId.Value);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation(
            "Client {ConnectionId} joined election package import session for user {UserId}",
            Context.ConnectionId,
            userId.Value);
    }

    /// <summary>
    /// Leaves the caller's personal package-import progress group.
    /// </summary>
    public async Task LeaveSession()
    {
        var userId = GetUserId(Context.User);
        if (userId == null)
        {
            return;
        }

        var groupName = GetGroupName(userId.Value);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation(
            "Client {ConnectionId} left election package import session for user {UserId}",
            Context.ConnectionId,
            userId.Value);
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "Client {ConnectionId} disconnected from ElectionPackageImportHub",
            Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Group name for one known teller's election package load progress.
    /// </summary>
    public static string GetGroupName(Guid userId) => $"ElectionPackageImport{userId}";

    /// <summary>
    /// Known (full) teller: same gate as MainHub multi-join (<c>isTeller</c> true = guest).
    /// </summary>
    private static bool IsKnownTeller(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
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
}
