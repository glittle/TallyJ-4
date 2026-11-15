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
/// <see cref="Allowed"/> and <see cref="ElectionExists"/> are independent:
/// a missing election can still be <see cref="Allowed"/> for a super admin,
/// and a present election is <see cref="Allowed"/> false when the caller
/// is not a member. Route handlers for <see cref="ElectionAccessPolicies.ElectionAccess"/>
/// and <see cref="ElectionAccessPolicies.FullTellerAccess"/> succeed when the
/// row is missing so the action can return 404.
/// </summary>
public readonly record struct ElectionAccessOutcome(bool Allowed, bool ElectionExists);

/// <summary>
/// Shared election membership checks for policies, resource-based
/// <c>IAuthorizationService.AuthorizeAsync</c> calls, and SignalR joins.
/// </summary>
public interface IElectionAccessEvaluator
{
    /// <summary>
    /// True when the JWT is an online voter (<c>voterType=online</c>).
    /// Those tokens fail every teller election policy and the default
    /// <c>[Authorize]</c> policy. Online voting stays on the OnlineVoter policy.
    /// </summary>
    bool IsOnlineVoter(ClaimsPrincipal? user);

    /// <summary>
    /// True when the email claim is in <c>SuperAdmin:Emails</c>.
    /// This is not the Identity <c>Admin</c> role and not an election Owner/Admin join.
    /// </summary>
    bool IsSuperAdmin(ClaimsPrincipal user);

    /// <summary>
    /// Membership for <paramref name="policyName"/>
    /// (<see cref="ElectionAccessPolicies"/>). Online voters are denied without
    /// a database read. Guest tellers pass only
    /// <see cref="ElectionAccessPolicies.ElectionAccess"/>, and only for the
    /// election guid on their token.
    /// </summary>
    Task<ElectionAccessOutcome> EvaluateAsync(ClaimsPrincipal user, Guid electionGuid, string policyName);

    /// <summary>
    /// True when <see cref="EvaluateAsync"/> allows
    /// <see cref="ElectionAccessPolicies.ElectionAccess"/>.
    /// Online voters and guests for another election are false. A member has
    /// no join row on a missing election, so they are false. Super admin and
    /// Identity Admin stay true when the row is missing; this method does not
    /// also require <see cref="ElectionAccessOutcome.ElectionExists"/>.
    /// The HTTP route handler is different: it succeeds for everyone when the
    /// row is missing so the action can return 404.
    /// </summary>
    Task<bool> CanJoinElectionAsync(ClaimsPrincipal? user, Guid electionGuid);
}

/// <summary>
/// Election membership for teller APIs. See <see cref="ElectionAccessPolicies"/>
/// for which policy allows a guest, a Teller join, or only Owner/Admin.
/// </summary>
public class ElectionAccessEvaluator : IElectionAccessEvaluator
{
    // Route values that hold the election guid on election-scoped actions.
    // `guid` is also the person and ballot id on those controllers; those
    // actions must use RequireElectionAccess, not this reader. A person guid
    // that is not an election id takes the missing-election success path on
    // the ElectionAccess route policy and would not check the person's election.
    // `id` is included only when it parses as a Guid, so a vote row id does not match.
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
            // Denied for every teller policy. Skip the election query;
            // ElectionExists stays false because this path did not look.
            return new ElectionAccessOutcome(false, false);
        }

        var electionExists = await _context.Elections
            .AsNoTracking()
            .AnyAsync(e => e.ElectionGuid == electionGuid);

        // Guest JWTs carry electionGuid for the passcode they used.
        // Only ElectionAccess honors it, and only for that one election.
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
                ElectionAccessPolicies.TellerNamesMemberAccess => await IsMemberOfElectionWithTellerNamesAsync(userId, electionGuid),
                // ElectionAccess and ElectionMemberAccess (policy string
                // HeadTellerAccess) both land here: any join row. An
                // unrecognized policy name does too; guest, super-admin, and
                // Identity-admin checks already ran.
                _ => await _context.JoinElectionUsers
                    .AsNoTracking()
                    .AnyAsync(j => j.ElectionGuid == electionGuid && j.UserId == userId)
            };

            if (allowed)
            {
                return new ElectionAccessOutcome(true, electionExists);
            }
        }

        if (await IsIdentityAdminAsync(user))
        {
            return new ElectionAccessOutcome(true, electionExists);
        }

        return new ElectionAccessOutcome(false, electionExists);
    }

    /// <summary>
    /// Reads the election guid from route values named
    /// <c>guid</c>, <c>electionGuid</c>, <c>electionId</c>, or <c>id</c>.
    /// The first value that parses as a <see cref="Guid"/> wins.
    /// </summary>
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

    // Teller rows are names entered on paper ballots. They are not joined to
    // AppUser. This is true when the account is a member and the election has
    // at least one of those names. Matching only on election id is intentional:
    // a teller name does not identify the logged-in account.
    private async Task<bool> IsMemberOfElectionWithTellerNamesAsync(Guid userId, Guid electionGuid)
    {
        return await _context.Tellers
            .AsNoTracking()
            .Join(
                _context.JoinElectionUsers.AsNoTracking(),
                teller => teller.ElectionGuid,
                membership => membership.ElectionGuid,
                (teller, membership) => new { teller.ElectionGuid, membership.UserId })
            .AnyAsync(x => x.ElectionGuid == electionGuid && x.UserId == userId);
    }

    // Identity role "Admin" is site-wide. The election join role "Admin" is a
    // different check, on JoinElectionUsers, used by FullTellerAccess.
    private async Task<bool> IsIdentityAdminAsync(ClaimsPrincipal user)
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
