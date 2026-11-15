using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

/// <summary>
/// <see cref="ElectionMemberAccessRequirement"/> from the route or from a
/// <see cref="Guid"/> resource. Same membership rule as
/// <see cref="ElectionAccessHandler"/> except a guest teller always fails.
/// Succeeds only when <see cref="ElectionAccessEvaluator.EvaluateAsync"/>
/// returns <see cref="ElectionAccessOutcome.Allowed"/>. A missing election
/// is not a separate failure: a normal member has no join row, so Allowed
/// is false, but a super admin or Identity Admin is still Allowed when the
/// row is missing. The audit-log caller turns a failure into 404.
/// </summary>
public class ElectionMemberAccessHandler : AuthorizationHandler<ElectionMemberAccessRequirement>
{
    private readonly IElectionAccessEvaluator _evaluator;

    public ElectionMemberAccessHandler(IElectionAccessEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ElectionMemberAccessRequirement requirement)
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
            ElectionAccessPolicies.ElectionMemberAccess);
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
