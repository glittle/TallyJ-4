using System.Text.Json;
using Compliance.General.Extensions;

namespace Backend.Middleware;

/// <summary>
/// Middleware that intercepts requests to /envConfig and serves environment-specific configuration.
/// The values served out are from appSettings in the "ForClientConfig" section, which should only contain non-sensitive settings needed by client-side code.
/// </summary>
public class WebEnvConfigMiddleware
{
  private readonly string? _cachedConfig;
  private readonly RequestDelegate _next;

  public WebEnvConfigMiddleware(RequestDelegate next, IConfiguration _configuration)
  {
    _next = next;

    // get the ForClientConfig setting section from whatever appSettings file was last loaded in program.cs
    var sectionDict =
      _configuration.GetSection("ForClientConfig").Get<Dictionary<string, object>>()
      ?? new Dictionary<string, object>();

    // ensure that the keys are all camel-cased
    var camelCaseDict = sectionDict.ToDictionary(
      pair => char.ToLowerInvariant(pair.Key[0]) + pair.Key.Substring(1),
      pair => pair.Value
    );

    // serialize the dictionary to a JSON string. Use indented to make debugging easier.
    _cachedConfig = JsonSerializer.Serialize(
      camelCaseDict,
      new JsonSerializerOptions { WriteIndented = true }
    );
  }

  public async Task InvokeAsync(HttpContext context)
  {
    // special handling for /envConfig to serve environment-specific config files
    if (
      _cachedConfig != null
      && context.Request.Path.Value?.Equals("/envConfig", StringComparison.OrdinalIgnoreCase)
        == true
    )
    {
      context.Response.ContentType = "application/json";
      await context.Response.WriteAsync(_cachedConfig);
      return;
      // Short-circuit ΓÇö do not continue to static files or other middleware
    }

    // Continue to next middleware for all other requests
    await _next(context);
  }
}
