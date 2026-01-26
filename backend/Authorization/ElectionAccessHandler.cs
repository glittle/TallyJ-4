using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TallyJ4.Domain.Context;

namespace TallyJ4.Authorization;

public class ElectionAccessHandler : AuthorizationHandler<ElectionAccessRequirement>
{
    private readonly MainDbContext _context;

    public ElectionAccessHandler(MainDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ElectionAccessRequirement requirement)
    {
        // Get the current user
        var user = context.User;
        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            context.Fail();
            return;
        }

        var userIdString = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            context.Fail();
            return;
        }

        // Extract election GUID from route parameters
        var routeData = context.Resource as RouteData;
        var actionDescriptor = context.Resource as ControllerActionDescriptor;

        Guid electionGuid;
        if (routeData != null)
        {
            // Try to get from route data
            if (routeData.Values.TryGetValue("guid", out var guidValue) &&
                Guid.TryParse(guidValue?.ToString(), out electionGuid))
            {
                // Check if user has access to this election
                var hasAccess = await _context.JoinElectionUsers
                    .AnyAsync(jeu => jeu.ElectionGuid == electionGuid && jeu.UserId == userId);

                if (hasAccess)
                {
                    context.Succeed(requirement);
                    return;
                }
            }
        }
        else if (actionDescriptor != null)
        {
            // Try to get from action descriptor route values
            if (actionDescriptor.RouteValues.TryGetValue("guid", out var guidValue) &&
                Guid.TryParse(guidValue, out electionGuid))
            {
                // Check if user has access to this election
                var hasAccess = await _context.JoinElectionUsers
                    .AnyAsync(jeu => jeu.ElectionGuid == electionGuid && jeu.UserId == userId);

                if (hasAccess)
                {
                    context.Succeed(requirement);
                    return;
                }
            }
        }

        context.Fail();
    }
}