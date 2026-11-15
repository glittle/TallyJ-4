using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

/// <summary>
/// Full teller: Identity Admin, super admin, or an Owner/Admin join row.
/// Guest tellers and online voters are denied. A super admin or Identity
/// Admin succeeds even when the route has no election guid.
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

        // Super admin is not tied to one election. Allow before looking at the route.
        if (_evaluator.IsSuperAdmin(user))
        {
            context.Succeed(requirement);
            return;
        }

        var httpContext = context.Resource as HttpContext;
        var routeData = context.Resource as RouteData ?? httpContext?.GetRouteData();
        if (!ElectionAccessEvaluator.TryGetElectionGuid(routeData, out var electionGuid))
        {
            // No election id on the route. EvaluateAsync(Guid.Empty) still
            // allows an Identity Admin, because that role check ignores the
            // guid. It does not treat a missing route value as a missing
            // election row. An Owner/Admin join stored under Guid.Empty would
            // pass the same way.
            var unscopedRouteOutcome = await _evaluator.EvaluateAsync(
                user,
                Guid.Empty,
                ElectionAccessPolicies.FullTellerAccess);
            if (unscopedRouteOutcome.Allowed)
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
        // can return 404. An existing election still requires Owner/Admin,
        // Identity Admin, or super admin.
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
