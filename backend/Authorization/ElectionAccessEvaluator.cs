using System.Security.Claims;
using Backend.Context;
using Backend.Helpers;
using Backend.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Authorization;

/// <summary>
/// Result of an election-scoped authorization check.
/// </summary>
public readonly record struct ElectionAccessOutcome(bool Allowed, bool ElectionExists);

/// <summary>
/// Shared election membership checks for policies, resource-based
/// <c>IAuthorizationService.AuthorizeAsync</c> calls, and SignalR joins.
/// </summary>
public interface IElectionAccessEvaluator
{
    bool IsOnlineVoter(ClaimsPrincipal? user);

    bool IsSuperAdmin(ClaimsPrincipal user);

    Task<ElectionAccessOutcome> EvaluateAsync(ClaimsPrincipal user, Guid electionGuid, string policyName);

    /// <summary>
    /// True when the caller may join an election SignalR group.
    /// A missing election is not joinable, unlike the route policy that lets
    /// the controller return 404.
    /// </summary>
    Task<bool> CanJoinElectionAsync(ClaimsPrincipal? user, Guid electionGuid);
}

/// <summary>
/// Election membership for teller APIs. Online-voter tokens never pass.
/// Guest tellers pass <see cref="ElectionAccessPolicies.ElectionAccess"/> only
/// for the election on their token. Super admins (email list) and Identity
/// Admins pass every election policy.
/// </summary>
public class ElectionAccessEvaluator : IElectionAccessEvaluator
{
    private static readonly string[] ElectionRouteKeys = ["guid", "electionGuid", "electionId", "id"];

    private readonly MainDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly SuperAdminSettings _superAdmin;

    public ElectionAccessEvaluator(
        MainDbContext context,
        UserManager<AppUser> userManager,
        IOptions<SuperAdminSettings> superAdmin)
    {
        _context = context;
        _userManager = userManager;
        _superAdmin = superAdmin.Value;
    }

    public static bool IsOnlineVoter(ClaimsPrincipal? user) =>
        string.Equals(user?.FindFirst("voterType")?.Value, "online", StringComparison.OrdinalIgnoreCase);

    bool IElectionAccessEvaluator.IsOnlineVoter(ClaimsPrincipal? user) => IsOnlineVoter(user);

    public bool IsSuperAdmin(ClaimsPrincipal user)
    {
        var email = user.FindFirst("email")?.Value
                    ?? user.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return false;
        }

        return _superAdmin.Emails.Any(configured =>
            string.Equals(configured, email, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> CanJoinElectionAsync(ClaimsPrincipal? user, Guid electionGuid)
    {
        if (user?.Identity?.IsAuthenticated != true || IsOnlineVoter(user))
        {
            return false;
        }

        var outcome = await EvaluateAsync(user, electionGuid, ElectionAccessPolicies.ElectionAccess);
        return outcome.Allowed;
    }

    public async Task<ElectionAccessOutcome> EvaluateAsync(
        ClaimsPrincipal user,
        Guid electionGuid,
        string policyName)
    {
        if (IsOnlineVoter(user))
        {
            return new ElectionAccessOutcome(false, false);
        }

        var electionExists = await _context.Elections
            .AsNoTracking()
            .AnyAsync(e => e.ElectionGuid == electionGuid);

        if (policyName == ElectionAccessPolicies.ElectionAccess
            && GuestTellerClaims.IsGuestTeller(user)
            && GuestTokenMatches(user, electionGuid))
        {
            return new ElectionAccessOutcome(true, electionExists);
        }

        if (GuestTellerClaims.IsGuestTeller(user))
        {
            return new ElectionAccessOutcome(false, electionExists);
        }

        if (IsSuperAdmin(user))
        {
            return new ElectionAccessOutcome(true, electionExists);
        }

        if (TryGetUserId(user, out var userId))
        {
            var allowed = policyName switch
            {
                ElectionAccessPolicies.FullTellerAccess => await _context.JoinElectionUsers
                    .AsNoTracking()
                    .AnyAsync(j => j.ElectionGuid == electionGuid
                                   && j.UserId == userId
                                   && (j.Role == "Owner" || j.Role == "Admin")),
                ElectionAccessPolicies.TellerAccess => await IsListedTellerAsync(userId, electionGuid),
                _ => await _context.JoinElectionUsers
                    .AsNoTracking()
                    .AnyAsync(j => j.ElectionGuid == electionGuid && j.UserId == userId)
            };

            if (allowed)
            {
                return new ElectionAccessOutcome(true, electionExists);
            }
        }

        if (await IsGlobalAdminAsync(user))
        {
            return new ElectionAccessOutcome(true, electionExists);
        }

        return new ElectionAccessOutcome(false, electionExists);
    }

    public static bool TryGetElectionGuid(RouteData? routeData, out Guid electionGuid)
    {
        electionGuid = Guid.Empty;
        if (routeData == null)
        {
            return false;
        }

        foreach (var key in ElectionRouteKeys)
        {
            if (routeData.Values.TryGetValue(key, out var guidValue) &&
                Guid.TryParse(guidValue?.ToString(), out electionGuid))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> IsListedTellerAsync(Guid userId, Guid electionGuid)
    {
        return await _context.Tellers
            .AsNoTracking()
            .Join(
                _context.JoinElectionUsers.AsNoTracking(),
                t => t.ElectionGuid,
                jeu => jeu.ElectionGuid,
                (t, jeu) => new { t.ElectionGuid, jeu.UserId })
            .AnyAsync(x => x.ElectionGuid == electionGuid && x.UserId == userId);
    }

    private async Task<bool> IsGlobalAdminAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        var appUser = await _userManager.FindByIdAsync(userId);
        return appUser != null && await _userManager.IsInRoleAsync(appUser, "Admin");
    }

    private static bool GuestTokenMatches(ClaimsPrincipal user, Guid electionGuid)
    {
        var electionGuidClaim = user.FindFirst("electionGuid")?.Value;
        return Guid.TryParse(electionGuidClaim, out var tokenElectionGuid)
               && tokenElectionGuid == electionGuid;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdString, out userId);
    }
}
