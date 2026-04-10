using Backend.Helpers;
using System.Text.Json;

namespace Backend.Middleware;

/// <summary>
/// Middleware that intercepts requests to /config.json and serves environment-specific config files.
/// For UAT environments, serves config-uat.json
/// For Prod environments, serves config-prod.json
/// For Dev environments, allows normal static file serving
/// </summary>
public class ConfigMiddleware
{
    private const string UatConfigFile = "config-uat.json";
    private const string ProdConfigFile = "config-prod.json";

    private readonly string? _cachedConfig;
    private readonly RequestDelegate _next;

    public ConfigMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;

        // Determine site type using the same logic as Program.cs
        var siteType = Environment.CommandLine.DetermineSiteType();

        // Pre-load config if not in dev environment
        if (siteType != "Dev")
        {
            string? configFile = null;
            switch (siteType)
            {
                case "UAT":
                    configFile = UatConfigFile;
                    break;
                case "Prod":
                    configFile = ProdConfigFile;
                    break;
            }
            if (configFile != null)
            {
                var filePath = Path.Combine(environment.WebRootPath, configFile);
                if (File.Exists(filePath))
                {
                    var content = File.ReadAllText(filePath);
                    try
                    {
                        JsonDocument.Parse(content);
                        _cachedConfig = content;
                    }
                    catch
                    {
                        // Invalid JSON, do not cache
                        _cachedConfig = null;
                    }
                }
            }
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // special handling for /config.json to serve environment-specific config files
        if (_cachedConfig != null && context.Request.Path.Value?.Equals("/config.json", StringComparison.OrdinalIgnoreCase) == true)
        {
            // config.json changes infrequently
            context.Response.Headers.Remove("Cache-Control");
            context.Response.Headers.Append("Cache-Control", "public, max-age=300"); // Cache for 5 minutes
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(_cachedConfig);
            return; // Short-circuit — do not continue to static files or other middleware
        }

        // Remove headers added by the framework
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("Server");

        // Block direct access to config files
        if (context.Request.Path.Value?.StartsWith("/config-", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.Response.StatusCode = 404;
            return;
        }

        // Add Hardening Headers
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Continue to next middleware for all other requests
        await _next(context);
    }
}