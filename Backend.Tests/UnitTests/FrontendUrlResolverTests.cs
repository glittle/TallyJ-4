using Backend.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Backend.Tests.UnitTests;

public class FrontendUrlResolverTests
{
    private static IHostEnvironment Env(string name)
    {
        var mock = new Mock<IHostEnvironment>();
        mock.Setup(e => e.EnvironmentName).Returns(name);
        return mock.Object;
    }

    private static IConfiguration Config(string? frontendUrl)
    {
        var values = new Dictionary<string, string?>();
        if (frontendUrl is not null)
        {
            values[FrontendUrlResolver.ConfigKey] = frontendUrl;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void ResolveBaseUri_DevelopmentMissingConfig_UsesDefault()
    {
        var uri = FrontendUrlResolver.ResolveBaseUri(Config(null), Env(Environments.Development));
        Assert.Equal(FrontendUrlResolver.DevelopmentDefault, uri.GetLeftPart(UriPartial.Authority));
    }

    [Fact]
    public void ResolveBaseUri_TestingMissingConfig_UsesDefault()
    {
        var uri = FrontendUrlResolver.ResolveBaseUri(Config(null), Env("Testing"));
        Assert.Equal(FrontendUrlResolver.DevelopmentDefault, uri.GetLeftPart(UriPartial.Authority));
    }

    [Fact]
    public void ResolveBaseUri_ProductionMissingConfig_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            FrontendUrlResolver.ResolveBaseUri(Config(null), Env(Environments.Production)));
    }

    [Fact]
    public void ResolveBaseUri_InvalidAbsoluteUrl_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            FrontendUrlResolver.ResolveBaseUri(Config("not-a-url"), Env(Environments.Development)));
    }

    [Fact]
    public void ResolveBaseUri_RejectsQueryOnBase()
    {
        Assert.Throws<InvalidOperationException>(() =>
            FrontendUrlResolver.ResolveBaseUri(Config("https://app.example.com?x=1"), Env(Environments.Development)));
    }

    [Fact]
    public void Build_EncodesSpecialCharactersInQuery()
    {
        var url = FrontendUrlResolver.Build(
            Config("https://app.example.com"),
            Env(Environments.Development),
            "/verify-email",
            ("email", "user+tag@example.com"),
            ("token", "a/b=c d"));

        Assert.StartsWith("https://app.example.com/verify-email?", url);

        var query = url.Split('?', 2)[1];
        var map = query.Split('&')
            .Select(p => p.Split('=', 2))
            .ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));

        Assert.Equal("user+tag@example.com", map["email"]);
        Assert.Equal("a/b=c d", map["token"]);
        // Values must be percent-encoded in the raw query (not raw + / = / space)
        Assert.DoesNotContain("user+tag@example.com", query);
        Assert.DoesNotContain("a/b=c d", query);
    }

    [Fact]
    public void Build_NormalizesPathWithoutLeadingSlash()
    {
        var url = FrontendUrlResolver.Build(
            Config("https://app.example.com"),
            Env(Environments.Development),
            "confirm-email-change",
            ("token", "abc"));

        Assert.Equal("https://app.example.com/confirm-email-change?token=abc", url);
    }

    [Theory]
    [InlineData("//evil.example/phish")]
    [InlineData("https://evil.example/phish")]
    [InlineData("/path?already=query")]
    [InlineData("/path#frag")]
    public void Build_RejectsUnsafePaths(string path)
    {
        Assert.Throws<ArgumentException>(() =>
            FrontendUrlResolver.Build(
                Config("https://app.example.com"),
                Env(Environments.Development),
                path));
    }

    [Fact]
    public void GetOrigin_StripsPathFromConfiguredBase()
    {
        var origin = FrontendUrlResolver.GetOrigin(
            Config("https://app.example.com/subdir"),
            Env(Environments.Development));

        Assert.Equal("https://app.example.com", origin);
    }
}
