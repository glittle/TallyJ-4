using Backend.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

/// <summary>
/// Shared join gate for election-scoped SignalR groups.
/// </summary>
internal static class ElectionHubAuthorization
{
    public static async Task EnsureCanJoinAsync(
        IElectionAccessEvaluator access,
        System.Security.Claims.ClaimsPrincipal? user,
        Guid electionGuid)
    {
        if (!await access.CanJoinElectionAsync(user, electionGuid))
        {
            throw new HubException("Access denied.");
        }
    }
}
