using System.Net;
using Backend.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Probes that miss every endpoint are counted. SPA history fallback and matched API
/// results are not.
/// </summary>
public class UnmatchedRequestPipelineTests : IClassFixture<UnmatchedRequestPipelineTests.SpaWebApplicationFactory>
{
    private readonly SpaWebApplicationFactory _factory;

    public UnmatchedRequestPipelineTests(SpaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnknownApiRoute_IsCountedOncePerNormalizedPath()
    {
        Reset();
        using var client = _factory.CreateClient();

        var first = await client.GetAsync("/api/account/billing?x=1");
        var second = await client.GetAsync("/API/Account/Billing?y=2");

        Assert.Equal(HttpStatusCode.NotFound, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        Assert.Equal(
            [new UnmatchedRequestCount("/api/account/billing", 2)],
            Drain());
    }

    [Fact]
    public async Task WrongVerbOnKnownAction_IsCountedAsMethodNotFound()
    {
        Reset();
        using var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/public/health", content: null);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(
            [new UnmatchedRequestCount("/api/public/health", 1)],
            Drain());
    }

    [Fact]
    public async Task MatchedApiResults_AreNotCounted()
    {
        Reset();
        using var client = _factory.CreateClient();

        var health = await client.GetAsync("/api/public/health");
        var profile = await client.GetAsync("/api/account/getMyProfile");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
        Assert.Empty(Drain());
    }

    [Fact]
    public async Task SpaFallbackRoutes_AreServedAndNotCounted()
    {
        Reset();
        using var client = _factory.CreateClient();

        var billing = await client.GetAsync("/account/billing");
        var index = await client.GetAsync("/index?x=1");
        var election = await client.GetAsync("/elections/not-a-server-route");
        var loginProbe = await client.GetAsync("/wp-login.php");
        var excluded = await client.GetAsync("/systemhealth");

        Assert.Equal(HttpStatusCode.OK, billing.StatusCode);
        Assert.Equal(HttpStatusCode.OK, index.StatusCode);
        Assert.Equal(HttpStatusCode.OK, election.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loginProbe.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, excluded.StatusCode);
        Assert.Equal(
            [new UnmatchedRequestCount("/systemhealth", 1)],
            Drain());
    }

    private void Reset() => _ = Tracker.Drain();

    private IReadOnlyList<UnmatchedRequestCount> Drain() => Tracker.Drain();

    private UnmatchedRequestTracker Tracker =>
        _factory.Services.GetRequiredService<UnmatchedRequestTracker>();

    /// <summary>
    /// The repo backend has no wwwroot. Production deploys the SPA's index.html there;
    /// this factory does the same so history fallback is the matched endpoint, not a 404.
    /// </summary>
    public sealed class SpaWebApplicationFactory : CustomWebApplicationFactory
    {
        private readonly string _webRoot = Path.Combine(
            Path.GetTempPath(),
            "tallyj-spa-" + Guid.NewGuid().ToString("N"));

        public SpaWebApplicationFactory()
        {
            Directory.CreateDirectory(_webRoot);
            File.WriteAllText(
                Path.Combine(_webRoot, "index.html"),
                "<!doctype html><html><head><title>tallyj-test</title></head><body></body></html>");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseWebRoot(_webRoot);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try
            {
                if (Directory.Exists(_webRoot))
                {
                    Directory.Delete(_webRoot, recursive: true);
                }
            }
            catch (IOException)
            {
                // The host can still be releasing the file. The temp directory is disposable.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
