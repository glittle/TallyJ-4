using System.Net;
using Backend.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.UnitTests;

public class ForwardedHeadersSetupTests
{
    [Fact]
    public void Configure_Default_TrustsInfrastructurePeersOnly()
    {
        var services = new ServiceCollection();
        ForwardedHeadersSetup.Configure(services, new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();
        Assert.True(ForwardedHeadersSetup.TryRead(
            provider,
            out var forwardsFor,
            out var forwardsProto,
            out var knownProxyCount,
            out var knownNetworkCount,
            out var knownIpNetworkCount,
            out var forwardLimit));

        Assert.True(forwardsFor);
        Assert.True(forwardsProto);
        Assert.Equal(0, knownProxyCount);
        Assert.True(knownIpNetworkCount > 0);
        Assert.Equal(knownIpNetworkCount, knownNetworkCount);
        Assert.Equal(1, forwardLimit);

        Assert.True(ForwardedHeadersSetup.IsInfrastructureAddress(IPAddress.Parse("127.0.0.1")));
        Assert.True(ForwardedHeadersSetup.IsInfrastructureAddress(IPAddress.Parse("::1")));
        Assert.True(ForwardedHeadersSetup.IsInfrastructureAddress(IPAddress.Parse("10.1.2.3")));
        Assert.True(ForwardedHeadersSetup.IsInfrastructureAddress(IPAddress.Parse("169.254.129.1")));
        Assert.True(ForwardedHeadersSetup.IsInfrastructureAddress(IPAddress.Parse("::ffff:169.254.129.1")));
        Assert.True(ForwardedHeadersSetup.IsInfrastructureAddress(IPAddress.Parse("fd00::1")));
        Assert.False(ForwardedHeadersSetup.IsInfrastructureAddress(IPAddress.Parse("203.0.113.10")));
    }

    [Fact]
    public void Configure_ExtraKnownProxy_IsRegistered()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:KnownProxies:0"] = "203.0.113.8"
            })
            .Build();

        var services = new ServiceCollection();
        ForwardedHeadersSetup.Configure(services, configuration);
        using var provider = services.BuildServiceProvider();

        Assert.True(ForwardedHeadersSetup.TryRead(
            provider,
            out _,
            out _,
            out var knownProxyCount,
            out _,
            out _,
            out _));
        Assert.Equal(1, knownProxyCount);
    }

    [Fact]
    public void Configure_WhenSwitchIsOff_DoesNotRegisterForwardedHeaders()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:TrustPlatformProxy"] = "false"
            })
            .Build();

        Assert.False(ForwardedHeadersSetup.TrustPlatformProxy(configuration));

        var services = new ServiceCollection();
        ForwardedHeadersSetup.Configure(services, configuration);

        Assert.False(ForwardedHeadersSetup.HasRegistration(services));
    }
}
