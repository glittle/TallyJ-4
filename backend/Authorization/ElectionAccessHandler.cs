using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TallyJ4.Domain.Context;

namespace TallyJ4.Authorization;

/// <summary>
/// Authorization handler that validates user access to elections based on the ElectionAccessRequirement.
/// Checks if the authenticated user is associated with the election specified in the route parameter.
/// </summary>
public class ElectionAccessHandler : AuthorizationHandler<ElectionAccessRequirement>
{
    private readonly MainDbContext _context;
    private readonly ILogger<ElectionAccessHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the ElectionAccessHandler.
    /// </summary>
    /// <param name="context">The main database context for accessing election user relationships.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public ElectionAccessHandler(MainDbContext context, ILogger<ElectionAccessHandler> logger)
    {
        _context = context;
        _logger = logger;
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
        _logger.LogWarning("***** ElectionAccessHandler.HandleRequirementAsync called *****");
        
        // Get the current user
        var user = context.User;
        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            _logger.LogWarning("ElectionAccess: User not authenticated");
            context.Fail();
            return;
        }

        var userIdString = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            _logger.LogWarning("ElectionAccess: Could not parse user ID from claims");
            context.Fail();
            return;
        }
        
        _logger.LogInformation("ElectionAccess: Checking access for user {UserId}", userId);

        // Extract election GUID from route parameters
        // In ASP.NET Core, the resource can be HttpContext, RouteData, or ControllerActionDescriptor
        var httpContext = context.Resource as HttpContext;
        var routeData = context.Resource as RouteData ?? httpContext?.GetRouteData();
        
        _logger.LogWarning("***** Resource type: {ResourceType}, RouteData available: {HasRouteData}", 
            context.Resource?.GetType().Name ?? "null", routeData != null);

        if (routeData == null)
        {
            _logger.LogWarning("ElectionAccess: No route data available");
            context.Fail();
            return;
        }

        // Try to get election GUID from route
        if (!routeData.Values.TryGetValue("guid", out var guidValue) ||
            !Guid.TryParse(guidValue?.ToString(), out var electionGuid))
        {
            _logger.LogWarning("ElectionAccess: Could not parse election GUID from route. Available route values: {RouteValues}", 
                string.Join(", ", routeData.Values.Select(kvp => $"{kvp.Key}={kvp.Value}")));
            context.Fail();
            return;
        }

        _logger.LogInformation("ElectionAccess: Checking access to election {ElectionGuid} for user {UserId}", electionGuid, userId);

        // Check if the election exists
        var electionExists = await _context.Elections
            .AnyAsync(e => e.ElectionGuid == electionGuid);

        // If election doesn't exist, allow request to proceed so controller can return 404
        if (!electionExists)
        {
            _logger.LogInformation("ElectionAccess: Election {ElectionGuid} does not exist, allowing request", electionGuid);
            context.Succeed(requirement);
            return;
        }

        // Check if user has access to this election
        var hasAccess = await _context.JoinElectionUsers
            .AnyAsync(jeu => jeu.ElectionGuid == electionGuid && jeu.UserId == userId);

        _logger.LogInformation("ElectionAccess: User {UserId} access to election {ElectionGuid}: {HasAccess}", userId, electionGuid, hasAccess);

        if (hasAccess)
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("ElectionAccess: User {UserId} denied access to election {ElectionGuid}", userId, electionGuid);
            context.Fail();
        }
    }
}