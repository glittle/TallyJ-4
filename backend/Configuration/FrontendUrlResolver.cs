using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;

namespace Backend.Configuration;

/// <summary>
/// Resolves the public SPA origin from <c>ClientEnv:frontendUrl</c> and builds absolute frontend links.
/// </summary>
internal static class FrontendUrlResolver
{
    public const string ConfigKey = "ClientEnv:frontendUrl";
    public const string DevelopmentDefault = "https://localhost:8095";

    /// <summary>
    /// Returns the public SPA origin (scheme://host[:port]) used for emails, OAuth redirects, and CORS.
    /// </summary>
    public static string GetOrigin(IConfiguration configuration, IHostEnvironment environment)
        => ResolveBaseUri(configuration, environment).GetLeftPart(UriPartial.Authority);

    /// <summary>
    /// Resolves and validates the configured frontend base URI.
    /// Outside Development/Testing, <c>ClientEnv:frontendUrl</c> is required.
    /// </summary>
    public static Uri ResolveBaseUri(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var raw = configuration[ConfigKey]?.Trim();

        if (string.IsNullOrWhiteSpace(raw))
        {
            if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            {
                raw = DevelopmentDefault;
            }
            else
            {
                throw new InvalidOperationException(
                    "ClientEnv:frontendUrl is required outside Development. " +
                    "Set ClientEnv:frontendUrl (or env ClientEnv__frontendUrl) to the public SPA origin.");
            }
        }

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException(
                $"ClientEnv:frontendUrl must be an absolute http(s) origin without query/fragment. Value: '{raw}'");
        }

        return uri;
    }

    /// <summary>
    /// Builds an absolute frontend URL: origin + root-relative path + safely encoded query string.
    /// </summary>
    public static string Build(
        IConfiguration configuration,
        IHostEnvironment environment,
        string path,
        params (string Key, string Value)[] query)
    {
        var origin = GetOrigin(configuration, environment);
        var relative = NormalizeRootRelativePath(path);
        var url = origin + relative;

        if (query is not { Length: > 0 })
        {
            return url;
        }

        var pairs = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in query)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Query parameter key must be non-empty.", nameof(query));
            }

            pairs[key] = value ?? string.Empty;
        }

        return QueryHelpers.AddQueryString(url, pairs);
    }

    internal static string NormalizeRootRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        var normalized = path.StartsWith('/') ? path : "/" + path;

        if (normalized.StartsWith("//", StringComparison.Ordinal) ||
            normalized.Contains("://", StringComparison.Ordinal) ||
            normalized.Contains('\\') ||
            normalized.Contains('?') ||
            normalized.Contains('#'))
        {
            throw new ArgumentException(
                "Path must be a root-relative path without query, fragment, or scheme.",
                nameof(path));
        }

        return normalized;
    }
}
