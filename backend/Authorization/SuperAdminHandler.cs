using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TallyJ4.Authorization;

/// <summary>
/// Authorization handler that validates whether the authenticated user is a super admin
/// by checking their email against the configured list of super admin emails.
/// </summary>
public class SuperAdminHandler : AuthorizationHandler<SuperAdminRequirement>
{
    private readonly SuperAdminSettings _settings;
    private readonly ILogger<SuperAdminHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the SuperAdminHandler.
    /// </summary>
    /// <param name="settings">The super admin settings containing the list of authorized emails.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SuperAdminHandler(IOptions<SuperAdminSettings> settings, ILogger<SuperAdminHandler> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Handles the authorization requirement by checking if the user's email is in the super admin list.
    /// </summary>
    /// <param name="context">The authorization handler context containing user and resource information.</param>
    /// <param name="requirement">The SuperAdminRequirement being evaluated.</param>
    /// <returns>A task representing the asynchronous authorization check.</returns>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SuperAdminRequirement requirement)
    {
        var user = context.User;
        if (user == null || user.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("SuperAdmin: User not authenticated");
            context.Fail();
            return Task.CompletedTask;
        }

        var email = user.FindFirst("email")?.Value
                    ?? user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("SuperAdmin: No email claim found for user");
            context.Fail();
            return Task.CompletedTask;
        }

        var isSuperAdmin = _settings.Emails
            .Any(e => string.Equals(e, email, StringComparison.OrdinalIgnoreCase));

        if (isSuperAdmin)
        {
            _logger.LogInformation("SuperAdmin: Access granted for {Email}", email);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("SuperAdmin: Access denied for {Email}", email);
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
