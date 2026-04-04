using System.Text;
using Backend.Helpers;

namespace Backend.Middleware;

/// <summary>
/// Middleware that intercepts requests to /config.json and serves environment-specific config files.
/// For UAT environments, serves config-uat.json
/// For Prod environments, serves config-prod.json
/// For Dev environments, allows normal static file serving
/// </summary>
public class ConfigMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;
    private readonly string _siteType;

    public ConfigMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;

        // Determine site type using the same logic as Program.cs
        _siteType = Environment.CommandLine.DetermineSiteType();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Value?.Equals("/config.json", StringComparison.OrdinalIgnoreCase) == true)
        {
            // For development, let static files middleware handle it normally
            if (_siteType == "Dev")
            {
                await _next(context);
                return;
            }

            // Determine which config file to serve based on site type
            string configFile;
            if (_siteType == "UAT")
            {
                configFile = "config-uat.json";
            }
            else if (_siteType == "Prod")
            {
                configFile = "config-prod.json";
            }
            else
            {
                // Unknown site type, let static files middleware handle it
                await _next(context);
                return;
            }

            // Build the path to the config file
            var filePath = Path.Combine(_environment.WebRootPath, configFile);

            if (File.Exists(filePath))
            {
                context.Response.ContentType = "application/json";
                await context.Response.SendFileAsync(filePath);
                return; // Short-circuit — do not continue to static files or other middleware
            }
        }

        // Continue to next middleware for all other requests
        await _next(context);
    }
}