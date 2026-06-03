namespace Backend.Middleware;

/// <summary>
/// Remove framework-added response headers and add security headers.
/// </summary>
public class HardeningMiddleware
{
    private readonly RequestDelegate _requestDelegate;

    public HardeningMiddleware(RequestDelegate requestDelegate)
    {
        _requestDelegate = requestDelegate;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            // Remove headers added by IIS
            context.Response.Headers.Remove("X-Powered-By");
            context.Response.Headers.Remove("Server");

            // Add security hardening headers
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            return Task.CompletedTask;
        });

        // Continue to next middleware for all other requests
        await _requestDelegate(context);
    }
}