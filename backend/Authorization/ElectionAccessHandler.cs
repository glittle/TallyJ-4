using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

/// <summary>
/// Validates <see cref="ElectionAccessRequirement"/> from the route or from a
/// <see cref="Guid"/> resource (resource-based <c>AuthorizeAsync</c>).
/// Guest tellers are allowed only for the election on their token.
/// A missing election succeeds on the route so the action can return 404.
/// A <see cref="Guid"/> resource does not get that exception: no membership is a failure.
/// </summary>
public class ElectionAccessHandler : AuthorizationHandler<ElectionAccessRequirement>
{
    private readonly IElectionAccessEvaluator _evaluator;

    public ElectionAccessHandler(IElectionAccessEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ElectionAccessRequirement requirement)
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
                ElectionAccessPolicies.ElectionAccess);
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

        var httpContext = context.Resource as HttpContext;
        var routeData = context.Resource as RouteData ?? httpContext?.GetRouteData();
        if (!ElectionAccessEvaluator.TryGetElectionGuid(routeData, out var electionGuid))
        {
            context.Fail();
            return;
        }

        var outcome = await _evaluator.EvaluateAsync(
            user,
            electionGuid,
            ElectionAccessPolicies.ElectionAccess);

        // Missing election: let the action return 404 instead of 403.
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
