using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using IPNetwork = System.Net.IPNetwork;

namespace Backend.Configuration;

/// <summary>
/// X-Forwarded-For and X-Forwarded-Proto for IIS, Azure App Service, nginx, and Apache.
/// Headers are applied only when the TCP peer is an infrastructure address or an
/// address listed in <c>ForwardedHeaders:KnownProxies</c>. A public client that
/// connects directly (IIS in-process, or Kestrel with no proxy) cannot spoof them.
/// </summary>
public static class ForwardedHeadersSetup
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>
    /// Loopback, RFC 1918, link-local, and IPv6 unique-local / link-local, plus the
    /// IPv4-mapped form of each IPv4 range. The forwarded-headers middleware also
    /// maps <c>::ffff:a.b.c.d</c> to IPv4 before this check.
    /// </summary>
    private static readonly IPNetwork[] InfrastructureNetworks = CreateInfrastructureNetworks();

    /// <summary>
    /// Default true. The trust boundary is the infrastructure ranges below, not every peer.
    /// Set false to ignore forwarded headers entirely.
    /// </summary>
    public static bool TrustPlatformProxy(IConfiguration configuration)
    {
        return configuration.GetValue($"{SectionName}:TrustPlatformProxy", true);
    }

    /// <summary>
    /// True when <paramref name="address"/> is a platform hop this host trusts without extra config.
    /// </summary>
    public static bool IsInfrastructureAddress(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (Contains(address))
        {
            return true;
        }

        // Same mapping the middleware does before KnownIPNetworks.Contains.
        return address.IsIPv4MappedToIPv6 && Contains(address.MapToIPv4());
    }

    /// <summary>
    /// Apply X-Forwarded-For and X-Forwarded-Proto from one hop, and only when the
    /// TCP peer is an infrastructure address (or an extra configured proxy).
    /// An empty known-network list would trust every peer; do not clear and leave it empty.
    /// </summary>
    public static void Configure(IServiceCollection services, IConfiguration configuration)
    {
        if (!TrustPlatformProxy(configuration))
        {
            return;
        }

        var extraProxies = configuration.GetSection($"{SectionName}:KnownProxies").Get<string[]>()
            ?? [];

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownProxies.Clear();
            // KnownNetworks and KnownIPNetworks are one list. Replace the loopback-only
            // default with the infrastructure ranges. Leaving the list empty trusts all peers.
            options.KnownIPNetworks.Clear();
            foreach (var network in InfrastructureNetworks)
            {
                options.KnownIPNetworks.Add(network);
            }

            AddExtraProxies(options, extraProxies);
            options.ForwardLimit = 1;
        });
    }

    private static void AddExtraProxies(ForwardedHeadersOptions options, string[] extraProxies)
    {
        foreach (var entry in extraProxies)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            var trimmed = entry.Trim();
            if (trimmed.Contains('/', StringComparison.Ordinal))
            {
                if (!IPNetwork.TryParse(trimmed, out var network))
                {
                    throw new InvalidOperationException(
                        $"ForwardedHeaders:KnownProxies entry '{trimmed}' is not a CIDR prefix.");
                }

                options.KnownIPNetworks.Add(network);
                continue;
            }

            if (!IPAddress.TryParse(trimmed, out var address))
            {
                throw new InvalidOperationException(
                    $"ForwardedHeaders:KnownProxies entry '{trimmed}' is not an IP address or CIDR prefix.");
            }

            options.KnownProxies.Add(address);
        }
    }

    private static bool Contains(IPAddress address)
    {
        foreach (var network in InfrastructureNetworks)
        {
            if (network.Contains(address))
            {
                return true;
            }
        }

        return false;
    }

    private static IPNetwork[] CreateInfrastructureNetworks()
    {
        (string Address, int Prefix)[] ipv4 =
        [
            ("127.0.0.0", 8),
            ("10.0.0.0", 8),
            ("172.16.0.0", 12),
            ("192.168.0.0", 16),
            ("169.254.0.0", 16)
        ];

        var networks = new List<IPNetwork>(ipv4.Length * 2 + 3);
        foreach (var (addressText, prefix) in ipv4)
        {
            var address = IPAddress.Parse(addressText);
            networks.Add(new IPNetwork(address, prefix));
            // IPv4-mapped form (::ffff:a.b.c.d). Prefix length is 96 plus the IPv4 prefix.
            networks.Add(new IPNetwork(address.MapToIPv6(), 96 + prefix));
        }

        networks.Add(new IPNetwork(IPAddress.IPv6Loopback, 128));
        networks.Add(new IPNetwork(IPAddress.Parse("fc00::"), 7));
        networks.Add(new IPNetwork(IPAddress.Parse("fe80::"), 10));
        return networks.ToArray();
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
