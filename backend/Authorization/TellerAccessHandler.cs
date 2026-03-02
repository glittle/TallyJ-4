using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Backend.Domain.Context;

namespace Backend.Authorization;

/// <summary>
/// Authorization handler that verifies if a user is a teller for an election.
/// </summary>
public class TellerAccessHandler : AuthorizationHandler<TellerAccessRequirement>
{
    private readonly MainDbContext _context;
    private readonly ILogger<TellerAccessHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TellerAccessHandler"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public TellerAccessHandler(MainDbContext context, ILogger<TellerAccessHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Handles the authorization requirement by checking if the user is a teller for the specified election.
    /// </summary>
    /// <param name="context">The authorization handler context.</param>
    /// <param name="requirement">The teller access requirement.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TellerAccessRequirement requirement)
    {
        var user = context.User;
        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            _logger.LogWarning("TellerAccess: User not authenticated");
            context.Fail();
            return;
        }

        var httpContext = context.Resource as HttpContext;
        var routeData = httpContext?.GetRouteData();

        if (routeData == null)
        {
            _logger.LogWarning("TellerAccess: No route data available");
            context.Fail();
            return;
        }

        if (!routeData.Values.TryGetValue("electionGuid", out var guidValue) ||
            !Guid.TryParse(guidValue?.ToString(), out var electionGuid))
        {
            _logger.LogWarning("TellerAccess: Could not parse election GUID from route");
            context.Fail();
            return;
        }

        // Check for guest teller (authenticated via access code)
        var isTellerClaim = user.FindFirst("isTeller")?.Value;
        var electionGuidClaim = user.FindFirst("electionGuid")?.Value;
        var authMethod = user.FindFirst("authMethod")?.Value;

        if (bool.TryParse(isTellerClaim, out var isGuestTeller) && isGuestTeller && 
            string.Equals(authMethod, "AccessCode", StringComparison.OrdinalIgnoreCase))
        {
            // Guest teller authenticated with access code
            if (Guid.TryParse(electionGuidClaim, out var tokenElectionGuid) && tokenElectionGuid == electionGuid)
            {
                _logger.LogInformation("TellerAccess: Guest teller authenticated for election {ElectionGuid}", electionGuid);
                context.Succeed(requirement);
                return;
            }
            else
            {
                _logger.LogWarning("TellerAccess: Guest teller token election GUID mismatch. Token: {TokenGuid}, Route: {RouteGuid}", electionGuidClaim, electionGuid);
                context.Fail();
                return;
            }
        }

        // Check for regular user teller (authenticated via user account)
        var userIdString = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            _logger.LogWarning("TellerAccess: Could not parse user ID from claims and not a guest teller");
            context.Fail();
            return;
        }

        var isTeller = await _context.Tellers
            .Join(_context.JoinElectionUsers,
                t => t.ElectionGuid,
                jeu => jeu.ElectionGuid,
                (t, jeu) => new { Teller = t, JoinElectionUser = jeu })
            .AnyAsync(x => x.Teller.ElectionGuid == electionGuid &&
                          x.JoinElectionUser.UserId == userId);

        if (isTeller)
        {
            _logger.LogInformation("TellerAccess: User {UserId} is a teller for election {ElectionGuid}", userId, electionGuid);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("TellerAccess: User {UserId} is not a teller for election {ElectionGuid}", userId, electionGuid);
            context.Fail();
        }
    }
}



