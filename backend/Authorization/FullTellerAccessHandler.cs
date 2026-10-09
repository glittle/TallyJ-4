using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

/// <summary>
/// Full teller: Identity Admin, super admin, or an Owner/Admin join row.
/// Guest tellers and online voters are denied. A global admin or super admin
/// succeeds even when the route has no election guid (existing admin bypass).
/// </summary>
public class FullTellerAccessHandler : AuthorizationHandler<FullTellerAccessRequirement>
{
    private readonly IElectionAccessEvaluator _evaluator;

    public FullTellerAccessHandler(IElectionAccessEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FullTellerAccessRequirement requirement)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true || _evaluator.IsOnlineVoter(user))
        {
            context.Fail();
            return;
        }

        if (context.Resource is Guid resourceElectionGuid)
        {
            var resourceOutcome = await _evaluator.EvaluateAsync(
                user,
                resourceElectionGuid,
                ElectionAccessPolicies.FullTellerAccess);
            if (resourceOutcome.Allowed)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return;
        }

        // Preserve the previous global-admin bypass for actions whose route
        // does not carry an election guid.
        if (_evaluator.IsSuperAdmin(user))
        {
            context.Succeed(requirement);
            return;
        }

        var httpContext = context.Resource as HttpContext;
        var routeData = context.Resource as RouteData ?? httpContext?.GetRouteData();
        if (!ElectionAccessEvaluator.TryGetElectionGuid(routeData, out var electionGuid))
        {
            var bypass = await _evaluator.EvaluateAsync(user, Guid.Empty, ElectionAccessPolicies.FullTellerAccess);
            if (bypass.Allowed)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return;
        }

        var outcome = await _evaluator.EvaluateAsync(
            user,
            electionGuid,
            ElectionAccessPolicies.FullTellerAccess);

        // Same as ElectionAccess: a missing election reaches the action so it
        // can return 404. An existing election still requires Owner/Admin.
        if (outcome.Allowed || !outcome.ElectionExists)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
