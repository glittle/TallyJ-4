using System.Security.Claims;

namespace TallyJ4.Middleware;

public class ElectionContextMiddleware
{
    private readonly RequestDelegate _next;

    public ElectionContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Try to extract election GUID from route parameters
        if (context.Request.RouteValues.TryGetValue("electionGuid", out var electionGuidValue) ||
            context.Request.RouteValues.TryGetValue("guid", out electionGuidValue) ||
            context.Request.RouteValues.TryGetValue("id", out electionGuidValue))
        {
            if (Guid.TryParse(electionGuidValue?.ToString(), out var electionGuid))
            {
                // Add election GUID to user claims for authorization
                var claimsIdentity = context.User.Identity as ClaimsIdentity;
                if (claimsIdentity != null)
                {
                    claimsIdentity.AddClaim(new Claim("ElectionGuid", electionGuid.ToString()));
                }
            }
        }

        await _next(context);
    }
}