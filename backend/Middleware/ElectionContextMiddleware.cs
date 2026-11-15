using System.Security.Claims;

namespace Backend.Middleware;

/// <summary>
/// Copies a route value onto the principal as claim <c>ElectionGuid</c> so
/// <see cref="AuditMiddleware"/> can store an election id on operational
/// audit rows. Authorization does not read this claim. Election policies
/// read route data, and entity routes use <c>RequireElectionAccessFilter</c>.
/// </summary>
/// <remarks>
/// Keys are tried in order: <c>electionGuid</c>, then <c>guid</c>, then
/// <c>id</c>. Only a value that parses as a <see cref="Guid"/> is copied.
/// Person and ballot routes are also named <c>guid</c>, so on those requests
/// the claim is that person or ballot id, not the election. Audit prefers
/// this claim over a route key named <c>electionGuid</c>.
/// </remarks>
public class ElectionContextMiddleware
{
    private readonly RequestDelegate _next;

    public ElectionContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.RouteValues.TryGetValue("electionGuid", out var routeValue) ||
            context.Request.RouteValues.TryGetValue("guid", out routeValue) ||
            context.Request.RouteValues.TryGetValue("id", out routeValue))
        {
            if (Guid.TryParse(routeValue?.ToString(), out var electionGuid))
            {
                if (context.User.Identity is ClaimsIdentity claimsIdentity)
                {
                    claimsIdentity.AddClaim(new Claim("ElectionGuid", electionGuid.ToString()));
                }
            }
        }

        await _next(context);
    }
}
