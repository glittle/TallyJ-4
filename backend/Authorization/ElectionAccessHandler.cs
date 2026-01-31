using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TallyJ4.Domain.Context;

namespace TallyJ4.Authorization;

/// <summary>
/// Authorization handler that validates user access to elections based on the ElectionAccessRequirement.
/// Checks if the authenticated user is associated with the election specified in the route parameter.
/// </summary>
public class ElectionAccessHandler : AuthorizationHandler<ElectionAccessRequirement>
{
    private readonly MainDbContext _context;

    /// <summary>
    /// Initializes a new instance of the ElectionAccessHandler.
    /// </summary>
    /// <param name="context">The main database context for accessing election user relationships.</param>
    public ElectionAccessHandler(MainDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Handles the authorization requirement by checking if the user has access to the specified election.
    /// </summary>
    /// <param name="context">The authorization handler context containing user and resource information.</param>
    /// <param name="requirement">The ElectionAccessRequirement being evaluated.</param>
    /// <returns>A task representing the asynchronous authorization check.</returns>
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

        var userIdString = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value;
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