using Microsoft.Extensions.Hosting;

namespace Backend.Configuration;

/// <summary>
/// Resolves the client/Sentry environment name advertised in <c>/clientEnv.json</c>.
/// </summary>
/// <remarks>
/// <c>backend/appsettings.json</c> defaults <c>ClientEnv:env</c> to <c>development</c> for local work.
/// Hosted sites (UAT, production) typically run with <c>ASPNETCORE_ENVIRONMENT=Production</c> and
/// inherit that default unless Azure App Settings override it. This resolver treats that inherited
/// <c>development</c> value as unset outside Development/Testing so Sentry is not tagged as a
/// local environment. An explicit non-development <c>ClientEnv:env</c> (or <c>ClientEnv__env</c>)
/// is always honored.
/// </remarks>
internal static class ClientEnvResolver
{
    public const string EnvConfigKey = "ClientEnv:env";
    public const string DevelopmentEnv = "development";
    public const string ProductionEnv = "production";
    public const string UatEnv = "uat";
    public const string UatFrontendHost = "uat.v4.tallyj.com";

    /// <summary>
    /// Returns the environment name the SPA should report (Sentry <c>environment</c>, sampling keys).
    /// </summary>
    public static string ResolveEnv(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var configured = configuration[EnvConfigKey]?.Trim();

        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            return string.IsNullOrWhiteSpace(configured) ? DevelopmentEnv : configured;
        }

        if (!string.IsNullOrWhiteSpace(configured) &&
            !string.Equals(configured, DevelopmentEnv, StringComparison.OrdinalIgnoreCase))
        {
            return configured;
        }

        return InferHostedEnv(configuration);
    }

    /// <summary>
    /// Hosted default when <c>ClientEnv:env</c> is missing, blank, or the repo <c>development</c> default.
    /// Prefer <c>uat</c> for the public UAT SPA host; otherwise <c>production</c>.
    /// </summary>
    internal static string InferHostedEnv(IConfiguration configuration)
    {
        var raw = configuration[FrontendUrlResolver.ConfigKey]?.Trim();
        if (Uri.TryCreate(raw, UriKind.Absolute, out var uri) &&
            string.Equals(uri.Host, UatFrontendHost, StringComparison.OrdinalIgnoreCase))
        {
            return UatEnv;
        }

        return ProductionEnv;
    }
}
