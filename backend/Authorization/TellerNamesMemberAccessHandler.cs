using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

/// <summary>
/// <see cref="TellerNamesMemberAccessRequirement"/> from the route or from a
/// <see cref="Guid"/> resource. Guest tellers fail even for the election on
/// their token; that exception exists only on <see cref="ElectionAccessHandler"/>.
/// </summary>
public class TellerNamesMemberAccessHandler : AuthorizationHandler<TellerNamesMemberAccessRequirement>
{
    private readonly IElectionAccessEvaluator _evaluator;

    public TellerNamesMemberAccessHandler(IElectionAccessEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TellerNamesMemberAccessRequirement requirement)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true || _evaluator.IsOnlineVoter(user))
        {
            context.Fail();
            return;
        }

        if (context.Resource is not Guid electionGuid)
        {
            var httpContext = context.Resource as HttpContext;
            var routeData = context.Resource as RouteData ?? httpContext?.GetRouteData();
            if (!ElectionAccessEvaluator.TryGetElectionGuid(routeData, out electionGuid))
            {
                context.Fail();
                return;
            }
        }

        var outcome = await _evaluator.EvaluateAsync(
            user,
            electionGuid,
            ElectionAccessPolicies.TellerNamesMemberAccess);
        if (outcome.Allowed)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
