using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

/// <summary>
/// Guest teller for the token's election, or a member of an election that has a teller row.
/// Online voters are denied.
/// </summary>
public class TellerAccessHandler : AuthorizationHandler<TellerAccessRequirement>
{
    private readonly IElectionAccessEvaluator _evaluator;

    public TellerAccessHandler(IElectionAccessEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TellerAccessRequirement requirement)
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
            ElectionAccessPolicies.TellerAccess);
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
