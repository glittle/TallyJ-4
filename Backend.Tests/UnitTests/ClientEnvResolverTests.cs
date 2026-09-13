using Backend.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Backend.Tests.UnitTests;

public class ClientEnvResolverTests
{
    private static IHostEnvironment Env(string name)
    {
        var mock = new Mock<IHostEnvironment>();
        mock.Setup(e => e.EnvironmentName).Returns(name);
        return mock.Object;
    }

    private static IConfiguration Config(string? env, string? frontendUrl = null)
    {
        var values = new Dictionary<string, string?>();
        if (env is not null)
        {
            values[ClientEnvResolver.EnvConfigKey] = env;
        }

        if (frontendUrl is not null)
        {
            values[FrontendUrlResolver.ConfigKey] = frontendUrl;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void ResolveEnv_Development_KeepsConfiguredDevelopment()
    {
        var env = ClientEnvResolver.ResolveEnv(
            Config("development", "https://localhost:8095"),
            Env(Environments.Development));

        Assert.Equal(ClientEnvResolver.DevelopmentEnv, env);
    }

    [Fact]
    public void ResolveEnv_Development_MissingConfig_DefaultsToDevelopment()
    {
        var env = ClientEnvResolver.ResolveEnv(Config(null), Env(Environments.Development));
        Assert.Equal(ClientEnvResolver.DevelopmentEnv, env);
    }

    [Fact]
    public void ResolveEnv_Testing_KeepsDevelopmentDefault()
    {
        var env = ClientEnvResolver.ResolveEnv(
            Config("development"),
            Env("Testing"));

        Assert.Equal(ClientEnvResolver.DevelopmentEnv, env);
    }

    [Fact]
    public void ResolveEnv_Production_UatFrontendHost_DoesNotEmitDevelopment()
    {
        var env = ClientEnvResolver.ResolveEnv(
            Config("development", "https://uat.v4.tallyj.com"),
            Env(Environments.Production));

        Assert.Equal(ClientEnvResolver.UatEnv, env);
    }

    [Fact]
    public void ResolveEnv_Production_UatFrontendHost_IsCaseInsensitive()
    {
        var env = ClientEnvResolver.ResolveEnv(
            Config("Development", "https://UAT.v4.tallyj.com/"),
            Env(Environments.Production));

        Assert.Equal(ClientEnvResolver.UatEnv, env);
    }

    [Fact]
    public void ResolveEnv_Production_OtherHost_DefaultsToProduction()
    {
        var env = ClientEnvResolver.ResolveEnv(
            Config("development", "https://v4.tallyj.com"),
            Env(Environments.Production));

        Assert.Equal(ClientEnvResolver.ProductionEnv, env);
    }

    [Fact]
    public void ResolveEnv_Production_MissingFrontendUrl_DefaultsToProduction()
    {
        var env = ClientEnvResolver.ResolveEnv(
            Config("development"),
            Env(Environments.Production));

        Assert.Equal(ClientEnvResolver.ProductionEnv, env);
    }

    [Fact]
    public void ResolveEnv_NonDevelopmentTestingHost_DoesNotEmitDevelopment()
    {
        var env = ClientEnvResolver.ResolveEnv(
            Config("development", "https://v4.tallyj.com"),
            Env("Staging"));

        Assert.Equal(ClientEnvResolver.ProductionEnv, env);
    }

    [Fact]
    public void ResolveEnv_Production_HonorsExplicitNonDevelopmentOverride()
    {
        var env = ClientEnvResolver.ResolveEnv(
            Config("staging", "https://uat.v4.tallyj.com"),
            Env(Environments.Production));

        Assert.Equal("staging", env);
    }

    [Fact]
    public void ResolveEnv_Production_HonorsExplicitUat()
    {
        var env = ClientEnvResolver.ResolveEnv(
            Config("uat", "https://v4.tallyj.com"),
            Env(Environments.Production));

        Assert.Equal(ClientEnvResolver.UatEnv, env);
    }

    [Fact]
    public void ResolveEnv_Production_BlankEnv_InfersFromHost()
    {
        var env = ClientEnvResolver.ResolveEnv(
            Config("   ", "https://uat.v4.tallyj.com"),
            Env(Environments.Production));

        Assert.Equal(ClientEnvResolver.UatEnv, env);
    }

    [Fact]
    public void InferHostedEnv_UatHost_ReturnsUat()
    {
        Assert.Equal(
            ClientEnvResolver.UatEnv,
            ClientEnvResolver.InferHostedEnv(Config(null, "https://uat.v4.tallyj.com")));
    }

    [Fact]
    public void InferHostedEnv_NonUatHost_ReturnsProduction()
    {
        Assert.Equal(
            ClientEnvResolver.ProductionEnv,
            ClientEnvResolver.InferHostedEnv(Config(null, "https://example.com")));
    }
}
