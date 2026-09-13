using System.Text.Json;
using Backend.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Backend.Tests.UnitTests;

public class ClientEnvMiddlewareTests
{
    private static IHostEnvironment Env(string name)
    {
        var mock = new Mock<IHostEnvironment>();
        mock.Setup(e => e.EnvironmentName).Returns(name);
        return mock.Object;
    }

    private static IConfiguration Config(string? env, string? frontendUrl, string? apiUrl = "https://example.com")
    {
        var values = new Dictionary<string, string?>
        {
            ["ClientEnv:apiUrl"] = apiUrl
        };

        if (env is not null)
        {
            values["ClientEnv:env"] = env;
        }

        if (frontendUrl is not null)
        {
            values["ClientEnv:frontendUrl"] = frontendUrl;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static async Task<JsonElement> GetClientEnvAsync(IConfiguration configuration, IHostEnvironment environment)
    {
        var middleware = new ClientEnvMiddleware(_ => Task.CompletedTask, configuration, environment);
        var context = new DefaultHttpContext();
        context.Request.Path = "/clientEnv.json";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.Clone();
    }

    [Fact]
    public async Task ClientEnvJson_Development_EmitsDevelopment()
    {
        var json = await GetClientEnvAsync(
            Config("development", "https://localhost:8095", "http://localhost:5016"),
            Env(Environments.Development));

        Assert.Equal("development", json.GetProperty("env").GetString());
        Assert.Equal("http://localhost:5016", json.GetProperty("apiUrl").GetString());
    }

    [Fact]
    public async Task ClientEnvJson_Testing_EmitsDevelopment()
    {
        var json = await GetClientEnvAsync(
            Config("development", "https://localhost:8095"),
            Env("Testing"));

        Assert.Equal("development", json.GetProperty("env").GetString());
    }

    [Fact]
    public async Task ClientEnvJson_ProductionUatHost_DoesNotEmitDevelopment()
    {
        var json = await GetClientEnvAsync(
            Config("development", "https://uat.v4.tallyj.com"),
            Env(Environments.Production));

        Assert.Equal("uat", json.GetProperty("env").GetString());
        Assert.NotEqual("development", json.GetProperty("env").GetString());
    }

    [Fact]
    public async Task ClientEnvJson_ProductionOtherHost_EmitsProduction()
    {
        var json = await GetClientEnvAsync(
            Config("development", "https://v4.tallyj.com"),
            Env(Environments.Production));

        Assert.Equal("production", json.GetProperty("env").GetString());
    }

    [Fact]
    public async Task ClientEnvJson_Production_ExplicitOverride_IsHonored()
    {
        var json = await GetClientEnvAsync(
            Config("uat", "https://v4.tallyj.com"),
            Env(Environments.Production));

        Assert.Equal("uat", json.GetProperty("env").GetString());
    }

    [Fact]
    public async Task ClientEnvJson_IgnoresOtherPaths()
    {
        var nextCalled = false;
        var middleware = new ClientEnvMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            Config("development", "https://uat.v4.tallyj.com"),
            Env(Environments.Production));

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/health";

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }
}
