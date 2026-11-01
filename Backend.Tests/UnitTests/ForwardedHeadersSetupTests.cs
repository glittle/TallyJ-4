using Backend.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.UnitTests;

public class ForwardedHeadersSetupTests
{
    [Fact]
    public void Configure_Default_TrustsOnePlatformHopForForAndProto()
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
        Assert.Equal(0, knownNetworkCount);
        Assert.Equal(0, knownIpNetworkCount);
        Assert.Equal(1, forwardLimit);
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
