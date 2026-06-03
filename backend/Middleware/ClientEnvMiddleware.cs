using System.Text.Json;

namespace Backend.Middleware;

/// <summary>
/// Middleware that intercepts requests to /clientEnv.json and serves environment-specific configuration.
/// The values served out are from appSettings in the "ClientEnv" section, which should only contain non-sensitive settings needed by client-side code.
/// </summary>
public class ClientEnvMiddleware
{
    private readonly string? _cachedConfig;
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    private string MakeClientEnv()
    {
        // get the ClientEnv setting section from whatever appSettings file was last loaded in program.cs
        var sectionDict =
          _configuration.GetSection("ClientEnv").Get<Dictionary<string, object>>()
          ?? new Dictionary<string, object>();

        // ensure that the keys are all camel-cased
        var camelCaseDict = sectionDict.ToDictionary(
          pair => char.ToLowerInvariant(pair.Key[0]) + pair.Key.Substring(1),
          pair => pair.Value
        );

        // serialize the dictionary to a JSON string. Use indented to make debugging easier.
        return JsonSerializer.Serialize(
          camelCaseDict,
          new JsonSerializerOptions { WriteIndented = true }
        );
    }

    public ClientEnvMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;

        // serialize the dictionary to a JSON string. Use indented to make debugging easier.
        // only if not in Development
        if (_configuration["ASPNETCORE_ENVIRONMENT"] != "Development")
        {
            _cachedConfig = MakeClientEnv();
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // special handling for /clientEnv.json to serve environment-specific config files
        if (
          context.Request.Path.Value?.Equals("/clientEnv.json", StringComparison.OrdinalIgnoreCase) ?? false
        )
        {
            var configToServe = _cachedConfig ?? MakeClientEnv();
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(configToServe);
            return;
            // Short-circuit — do not continue to static files or other middleware
        }

        // Continue to next middleware for all other requests
        await _next(context);
    }
}