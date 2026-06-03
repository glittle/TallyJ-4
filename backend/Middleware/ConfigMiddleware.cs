// using Backend.Helpers;
// using Microsoft.Extensions.Configuration;
// using System.Text.Json;

// namespace Backend.Middleware;

// /// <summary>
// /// Middleware that intercepts requests to /config.json and serves environment-specific config files.
// /// 
// /// Behavior by site type (determined from command line the same way as Program.cs):
// /// 
// /// • UAT: Only serves config-uat.json (from wwwroot). Fails (404) if the file is missing.
// /// • Prod: Only serves config-prod.json (from wwwroot). Fails (404) if the file is missing.
// /// • Dev / local / debug: 
// ///     - Serves config.json if present (highest priority override)
// ///     - Falls back to config-dev.json, then config-prod.json / config-uat.json for convenience
// ///     - As a last resort, dynamically generates a minimal config (only for local `npm run preview` testing)
// /// 
// /// Cross-environment fallback is intentionally disallowed for UAT and Prod.
// /// </summary>
// public class ConfigMiddleware
// {
//     private const string UatConfigFile = "config-uat.json";
//     private const string ProdConfigFile = "config-prod.json";
//     private const string DevConfigFile = "config-dev.json";

//     private readonly string? _cachedConfig;
//     private readonly RequestDelegate _next;
//     private readonly IConfiguration? _configuration;
//     private readonly IWebHostEnvironment _environment;
//     private readonly string _siteType;

//     public ConfigMiddleware(RequestDelegate next, IWebHostEnvironment environment, IConfiguration configuration)
//     {
//         _next = next;
//         _environment = environment;
//         _configuration = configuration;

//         // Determine site type using the same logic as Program.cs
//         _siteType = Environment.CommandLine.DetermineSiteType();

//         // Always attempt to preload a config file for /config.json.
//         // This supports local development scenarios (npm run preview + local backend on 5016)
//         // as well as real Prod/UAT deployments.
//         _cachedConfig = TryLoadConfigFile();
//     }

//     private string? TryLoadConfigFile()
//     {
//         if (_environment.WebRootPath == null)
//             return null;

//         var candidates = new List<string>();

//         // Highest priority: explicit override (useful for local testing or ops hotfixes)
//         candidates.Add("config.json");

//         // IMPORTANT: Real environments (UAT/Prod) are strict — no cross-environment fallback.
//         // UAT must fail if config-uat.json is missing. Prod must fail if config-prod.json is missing.
//         switch (_siteType)
//         {
//             case "UAT":
//                 candidates.Add(UatConfigFile);      // strictly UAT only
//                 break;
//             case "Prod":
//                 candidates.Add(ProdConfigFile);     // strictly Prod only
//                 break;
//             default: // Dev / debug / local development
//                 candidates.Add(DevConfigFile);
//                 break;
//         }

//         foreach (var fileName in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
//         {
//             var filePath = Path.Combine(_environment.WebRootPath, fileName);
//             if (File.Exists(filePath))
//             {
//                 var content = File.ReadAllText(filePath);
//                 try
//                 {
//                     JsonDocument.Parse(content); // validate it's real JSON
//                     return content;
//                 }
//                 catch
//                 {
//                     // Invalid JSON — skip this candidate
//                 }
//             }
//         }

//         return null;
//     }

//     public async Task InvokeAsync(HttpContext context)
//     {
//         var path = context.Request.Path.Value;

//         // Handle /config.json requests
//         if (path?.Equals("/config.json", StringComparison.OrdinalIgnoreCase) == true)
//         {
//             string? responseBody = _cachedConfig;

//             // Last-resort: dynamically generate a minimal config **only in local Dev/debug** scenarios.
//             // This makes `npm run preview` + local backend "just work".
//             // Real UAT/Prod environments must never fall back to generated config — they must have their file.
//             if (responseBody == null && _siteType == "Dev")
//             {
//                 responseBody = GenerateDevelopmentConfig();
//             }

//             if (responseBody != null)
//             {
//                 context.Response.Headers.Remove("Cache-Control");
//                 context.Response.Headers.Append("Cache-Control", "public, max-age=60"); // short cache for dev-generated
//                 context.Response.ContentType = "application/json";
//                 await context.Response.WriteAsync(responseBody);
//                 return;
//             }

//             // No config available at all — fall through (will likely 404 from static files)
//         }

//         // Remove headers added by the framework
//         context.Response.Headers.Remove("X-Powered-By");
//         context.Response.Headers.Remove("Server");

//         // Block direct access to the raw config-*.json files (security)
//         if (path?.StartsWith("/config-", StringComparison.OrdinalIgnoreCase) == true)
//         {
//             context.Response.StatusCode = 404;
//             return;
//         }

//         // Add Hardening Headers
//         context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
//         context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
//         context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

//         // Continue to next middleware for all other requests
//         await _next(context);
//     }

//     /// <summary>
//     /// Generates a minimal frontend AppConfig on the fly.
//     /// Only ever called for local Dev/debug scenarios (guarded in InvokeAsync).
//     /// </summary>
//     private string GenerateDevelopmentConfig()
//     {
//         var apiUrl = _configuration?["Frontend:ApiUrl"]
//                      ?? _configuration?["ApiUrl"]
//                      ?? "http://localhost:5016";

//         // Try to read Google client id from the standard section
//         var googleClientId = _configuration?["Google:ClientId"];

//         var config = new
//         {
//             apiUrl = apiUrl,
//             env = "development",
//             sentryDsn = _configuration?["Sentry:Dsn"] ?? "",
//             googleClientId = string.IsNullOrWhiteSpace(googleClientId) ? null : googleClientId
//         };

//         return JsonSerializer.Serialize(config, new JsonSerializerOptions
//         {
//             PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//             WriteIndented = false,
//             DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
//         });
//     }
// }