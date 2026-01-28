using System.Security.Claims;

namespace TallyJ4.Middleware;

/// <summary>
/// Middleware that extracts election GUID from route parameters and adds it to user claims.
/// This enables authorization handlers to validate election-specific access permissions.
/// </summary>
public class ElectionContextMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the ElectionContextMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public ElectionContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the HTTP request and extracts election GUID from route parameters.
    /// Adds the election GUID to user claims for downstream authorization checks.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
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