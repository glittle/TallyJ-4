using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

/// <summary>
/// Any signed-in member of the election (JoinElectionUser). Guest tellers and
/// online voters are denied. Despite the name, this is not a separate head-teller role.
/// </summary>
public class HeadTellerAccessHandler : AuthorizationHandler<HeadTellerAccessRequirement>
{
    private readonly IElectionAccessEvaluator _evaluator;

    public HeadTellerAccessHandler(IElectionAccessEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HeadTellerAccessRequirement requirement)
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
            ElectionAccessPolicies.HeadTellerAccess);
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
