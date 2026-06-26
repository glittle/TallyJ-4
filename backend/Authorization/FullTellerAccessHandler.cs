using System.Security.Claims;
using Backend.Context;
using Backend.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Authorization;

/// <summary>
/// Authorization handler that restricts access to election administration actions
/// to FullTellers only.
/// </summary>
public class FullTellerAccessHandler : AuthorizationHandler<FullTellerAccessRequirement>
{
    private readonly MainDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<FullTellerAccessHandler> _logger;

    public FullTellerAccessHandler(
        MainDbContext context,
        UserManager<AppUser> userManager,
        ILogger<FullTellerAccessHandler> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FullTellerAccessRequirement requirement)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("FullTellerAccess: User not authenticated");
            context.Fail();
            return;
        }

        if (IsGuestTeller(user))
        {
            _logger.LogWarning("FullTellerAccess: GuestTeller denied");
            context.Fail();
            return;
        }

        var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            _logger.LogWarning("FullTellerAccess: Could not parse user ID from claims");
            context.Fail();
            return;
        }

        var appUser = await _userManager.FindByIdAsync(userIdString);
        if (appUser != null && await _userManager.IsInRoleAsync(appUser, "Admin"))
        {
            _logger.LogInformation("FullTellerAccess: Global Admin {UserId} authorized", userId);
            context.Succeed(requirement);
            return;
        }

        var httpContext = context.Resource as HttpContext;
        var routeData = context.Resource as RouteData ?? httpContext?.GetRouteData();
        if (routeData == null || !TryGetElectionGuidFromRoute(routeData, out var electionGuid))
        {
            _logger.LogWarning("FullTellerAccess: Could not parse election GUID from route");
            context.Fail();
            return;
        }

        var joinRecord = await _context.JoinElectionUsers
            .FirstOrDefaultAsync(j => j.ElectionGuid == electionGuid && j.UserId == userId);

        if (joinRecord is { Role: "Owner" or "Admin" })
        {
            _logger.LogInformation(
                "FullTellerAccess: User {UserId} authorized for election {ElectionGuid} with role {Role}",
                userId,
                electionGuid,
                joinRecord.Role);
            context.Succeed(requirement);
            return;
        }

        _logger.LogWarning(
            "FullTellerAccess: User {UserId} denied for election {ElectionGuid}",
            userId,
            electionGuid);
        context.Fail();
    }

    private static bool TryGetElectionGuidFromRoute(RouteData routeData, out Guid electionGuid)
    {
        electionGuid = Guid.Empty;

        foreach (var key in new[] { "guid", "electionGuid", "id" })
        {
            if (routeData.Values.TryGetValue(key, out var guidValue) &&
                Guid.TryParse(guidValue?.ToString(), out electionGuid))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGuestTeller(ClaimsPrincipal user)
    {
        var isTellerClaim = user.FindFirst("isTeller")?.Value;
        var authMethod = user.FindFirst("authMethod")?.Value;

        return bool.TryParse(isTellerClaim, out var isGuestTeller) && isGuestTeller
               && string.Equals(authMethod, "AccessCode", StringComparison.OrdinalIgnoreCase);
    }
}