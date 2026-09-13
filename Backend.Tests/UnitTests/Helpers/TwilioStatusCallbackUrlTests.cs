using Backend.Helpers;
using Microsoft.Extensions.Configuration;

namespace Backend.Tests.UnitTests.Helpers;

public class TwilioStatusCallbackUrlTests
{
    [Fact]
    public void TryResolve_ExplicitFullUrl_UsedAsIs()
    {
        var url = TwilioStatusCallbackUrl.TryResolve(Config(
            ("Twilio:StatusCallbackUrl", "https://uat.v4.tallyj.com/api/Public/smsStatus")));

        Assert.Equal("https://uat.v4.tallyj.com/api/Public/smsStatus", url);
    }

    [Fact]
    public void TryResolve_ExplicitOriginOnly_AppendsPath()
    {
        var url = TwilioStatusCallbackUrl.TryResolve(Config(
            ("Twilio:StatusCallbackUrl", "https://uat.v4.tallyj.com")));

        Assert.Equal("https://uat.v4.tallyj.com/api/Public/smsStatus", url);
    }

    [Fact]
    public void TryResolve_PlaceholderExplicit_FallsBackToApiUrl()
    {
        var url = TwilioStatusCallbackUrl.TryResolve(Config(
            ("Twilio:StatusCallbackUrl", "<TWILIO-CALLBACK-URL>"),
            ("ClientEnv:apiUrl", "http://localhost:5016")));

        Assert.Equal("http://localhost:5016/api/Public/smsStatus", url);
    }

    [Fact]
    public void TryResolve_ApiUrl_ComposesPath()
    {
        var url = TwilioStatusCallbackUrl.TryResolve(Config(
            ("ClientEnv:apiUrl", "https://api.example.com")));

        Assert.Equal("https://api.example.com/api/Public/smsStatus", url);
    }

    [Fact]
    public void TryResolve_FrontendUrlOnly_ComposesPath()
    {
        var url = TwilioStatusCallbackUrl.TryResolve(Config(
            ("ClientEnv:frontendUrl", "https://uat.v4.tallyj.com")));

        Assert.Equal("https://uat.v4.tallyj.com/api/Public/smsStatus", url);
    }

    [Fact]
    public void TryResolve_ExplicitBeatsApiUrl()
    {
        var url = TwilioStatusCallbackUrl.TryResolve(Config(
            ("Twilio:StatusCallbackUrl", "https://hooks.example.com/api/Public/smsStatus"),
            ("ClientEnv:apiUrl", "https://api.example.com")));

        Assert.Equal("https://hooks.example.com/api/Public/smsStatus", url);
    }

    [Fact]
    public void TryResolve_QueryOnUrl_Rejected()
    {
        var url = TwilioStatusCallbackUrl.TryResolve(Config(
            ("Twilio:StatusCallbackUrl", "https://example.com/callback?x=1")));

        Assert.Null(url);
    }

    [Fact]
    public void TryResolve_NothingConfigured_Null()
    {
        Assert.Null(TwilioStatusCallbackUrl.TryResolve(Config()));
    }

    private static IConfiguration Config(params (string Key, string? Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => v.Value))
            .Build();
    }
}
