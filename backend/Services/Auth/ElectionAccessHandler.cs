using System.Security.Claims;
using Backend.Domain.Context;
using Backend.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Auth;

/// <summary>
/// Authorization handler for election-specific access control.
/// Checks if a user has permission to access election resources based on their role and election membership.
/// </summary>
public class ElectionAccessHandler : AuthorizationHandler<ElectionAccessRequirement>
{
    private readonly MainDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    /// <summary>
    /// Initializes a new instance of the ElectionAccessHandler.
    /// </summary>
    /// <param name="context">The main database context.</param>
    /// <param name="userManager">The user manager for identity operations.</param>
    public ElectionAccessHandler(MainDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <summary>
    /// Handles the authorization requirement by checking user permissions for election access.
    /// Grants access if user is admin or has appropriate election-specific permissions.
    /// </summary>
    /// <param name="context">The authorization handler context.</param>
    /// <param name="requirement">The election access requirement.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ElectionAccessRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return;
        }

        // Check if user is admin (global access)
        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        // Check election-specific access
        var electionGuidClaim = context.User.FindFirst("ElectionGuid")?.Value;
        if (!string.IsNullOrEmpty(electionGuidClaim) && Guid.TryParse(electionGuidClaim, out var electionGuid))
        {
            var joinRecord = await _context.JoinElectionUsers
                .FirstOrDefaultAsync(j => j.ElectionGuid == electionGuid && string.Equals(j.UserId, userId));

            if (joinRecord != null)
            {
                // Check if specific role is required
                if (string.IsNullOrEmpty(requirement.RequiredRole) ||
                    joinRecord.Role == requirement.RequiredRole)
                {
                    context.Succeed(requirement);
                    return;
                }
            }
        }

        // If we get here, access is denied
    }
}


