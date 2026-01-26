using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TallyJ4.Domain.Context;
using TallyJ4.Domain.Entities;
using TallyJ4.Domain.Identity;

namespace TallyJ4.Services.Auth;

public class ElectionAccessHandler : AuthorizationHandler<ElectionAccessRequirement>
{
    private readonly MainDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public ElectionAccessHandler(MainDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

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