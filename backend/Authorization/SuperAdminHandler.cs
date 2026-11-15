using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.Authorization;

/// <summary>
/// Succeeds when the caller's email claim is in <c>SuperAdmin:Emails</c>.
/// This is the site-wide list, not the Identity <c>Admin</c> role and not an
/// election Owner/Admin join. Email match is ordinal and case-insensitive.
/// The claim is <c>email</c>, then <see cref="System.Security.Claims.ClaimTypes.Email"/>.
/// </summary>
public class SuperAdminHandler : AuthorizationHandler<SuperAdminRequirement>
{
    private readonly SuperAdminSettings _settings;
    private readonly ILogger<SuperAdminHandler> _logger;

    public SuperAdminHandler(IOptions<SuperAdminSettings> settings, ILogger<SuperAdminHandler> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

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
            .Any(configured => string.Equals(configured, email, StringComparison.OrdinalIgnoreCase));

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
