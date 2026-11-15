using Backend.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

/// <summary>
/// Join gate for election-scoped SignalR groups. Denial throws
/// <see cref="HubException"/> with "Access denied." The decision is
/// <see cref="IElectionAccessEvaluator.CanJoinElectionAsync"/>: guest tellers
/// only for the election on their token, online voters never.
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
