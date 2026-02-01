using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TallyJ4.Domain.Context;

namespace TallyJ4.Authorization;

public class HeadTellerAccessHandler : AuthorizationHandler<HeadTellerAccessRequirement>
{
    private readonly MainDbContext _context;
    private readonly ILogger<HeadTellerAccessHandler> _logger;

    public HeadTellerAccessHandler(MainDbContext context, ILogger<HeadTellerAccessHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HeadTellerAccessRequirement requirement)
    {
        var user = context.User;
        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            _logger.LogWarning("HeadTellerAccess: User not authenticated");
            context.Fail();
            return;
        }

        var userIdString = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            _logger.LogWarning("HeadTellerAccess: Could not parse user ID from claims");
            context.Fail();
            return;
        }

        var httpContext = context.Resource as HttpContext;
        var routeData = httpContext?.GetRouteData();

        if (routeData == null)
        {
            _logger.LogWarning("HeadTellerAccess: No route data available");
            context.Fail();
            return;
        }

        if (!routeData.Values.TryGetValue("electionGuid", out var guidValue) ||
            !Guid.TryParse(guidValue?.ToString(), out var electionGuid))
        {
            _logger.LogWarning("HeadTellerAccess: Could not parse election GUID from route");
            context.Fail();
            return;
        }

        var isHeadTeller = await _context.Tellers
            .Join(_context.JoinElectionUsers,
                t => t.ElectionGuid,
                jeu => jeu.ElectionGuid,
                (t, jeu) => new { Teller = t, JoinElectionUser = jeu })
            .AnyAsync(x => x.Teller.ElectionGuid == electionGuid && 
                          x.JoinElectionUser.UserId == userId &&
                          x.Teller.IsHeadTeller == true);

        if (isHeadTeller)
        {
            _logger.LogInformation("HeadTellerAccess: User {UserId} is a head teller for election {ElectionGuid}", userId, electionGuid);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("HeadTellerAccess: User {UserId} is not a head teller for election {ElectionGuid}", userId, electionGuid);
            context.Fail();
        }
    }
}
