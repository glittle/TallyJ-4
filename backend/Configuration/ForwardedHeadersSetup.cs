using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Backend.Configuration;

/// <summary>
/// X-Forwarded-For and X-Forwarded-Proto for IIS, Azure App Service, nginx, and Apache.
/// </summary>
public static class ForwardedHeadersSetup
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>
    /// Default true. Set false only when clients connect directly and can set the headers themselves.
    /// </summary>
    public static bool TrustPlatformProxy(IConfiguration configuration)
    {
        return configuration.GetValue($"{SectionName}:TrustPlatformProxy", true);
    }

    /// <summary>
    /// Trust the platform proxy by clearing KnownNetworks and KnownProxies.
    /// ForwardLimit stays 1 so only the immediate hop is applied. A longer walk
    /// would continue into a client-supplied prefix.
    /// </summary>
    public static void Configure(IServiceCollection services, IConfiguration configuration)
    {
        if (!TrustPlatformProxy(configuration))
        {
            return;
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
#pragma warning disable ASPDEPR005
            // .NET 10 dual-list: clear both so platform hops are trusted (issue #63627).
            options.KnownNetworks.Clear();
#pragma warning restore ASPDEPR005
            options.ForwardLimit = 1;
        });
    }

    /// <summary>
    /// True when <see cref="Configure"/> registered forwarded-header options.
    /// </summary>
    public static bool HasRegistration(IServiceCollection services)
    {
        return services.Any(descriptor => descriptor.ServiceType == typeof(IConfigureOptions<ForwardedHeadersOptions>));
    }

    /// <summary>
    /// Reads the options <see cref="Configure"/> registered. Returns false when the switch is off.
    /// </summary>
    public static bool TryRead(
        IServiceProvider provider,
        out bool forwardsFor,
        out bool forwardsProto,
        out int knownProxyCount,
        out int knownNetworkCount,
        out int knownIpNetworkCount,
        out int? forwardLimit)
    {
        var options = provider.GetService<IOptions<ForwardedHeadersOptions>>()?.Value;
        if (options is null)
        {
            forwardsFor = false;
            forwardsProto = false;
            knownProxyCount = 0;
            knownNetworkCount = 0;
            knownIpNetworkCount = 0;
            forwardLimit = null;
            return false;
        }

        forwardsFor = options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedFor);
        forwardsProto = options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedProto);
        knownProxyCount = options.KnownProxies.Count;
#pragma warning disable ASPDEPR005
        knownNetworkCount = options.KnownNetworks.Count;
#pragma warning restore ASPDEPR005
        knownIpNetworkCount = options.KnownIPNetworks.Count;
        forwardLimit = options.ForwardLimit;
        return true;
    }
}
