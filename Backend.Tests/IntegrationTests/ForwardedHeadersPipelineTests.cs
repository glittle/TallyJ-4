using System.Net;
using System.Net.Http.Json;
using Backend.DTOs.Auth;
using Backend.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Runs the real forwarded-headers middleware and the login rate limiter.
/// The test sets the TCP peer before that middleware; TestServer's own peer is loopback,
/// which is a trusted hop and would hide a direct-client spoof.
/// </summary>
public class ForwardedHeadersPipelineTests : IClassFixture<ForwardedHeadersPipelineTests.Factory>
{
    public const string RemoteIpHeader = "X-Test-Connection-Remote-Ip";

    private readonly HttpClient _client;

    public ForwardedHeadersPipelineTests(Factory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_PublicPeer_SpoofedXForwardedFor_StaysOnThePeerBucket()
    {
        const string peer = "198.51.100.10";

        HttpResponseMessage? last = null;
        for (var i = 0; i < RateLimitingMiddleware.LoginIpMaxRequests + 1; i++)
        {
            last = await PostLoginAsync(peer, $"203.0.113.{i + 1}");
            await Task.Delay(50);
        }

        last!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var otherPeer = await PostLoginAsync("198.51.100.11", "203.0.113.250");
        otherPeer.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Login_LinkLocalPeer_UsesForwardedClient_NotThePeer()
    {
        const string azurePeer = "::ffff:169.254.129.1";
        const string client = "203.0.113.44";

        HttpResponseMessage? last = null;
        for (var i = 0; i < RateLimitingMiddleware.LoginIpMaxRequests + 1; i++)
        {
            // Leftmost changes every call. The rightmost public address is the client.
            last = await PostLoginAsync(azurePeer, $"198.51.100.{i + 1}, {client}");
            await Task.Delay(50);
        }

        last!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var otherClient = await PostLoginAsync(azurePeer, "198.51.100.200, 203.0.113.55");
        otherClient.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    private async Task<HttpResponseMessage> PostLoginAsync(string remoteIp, string forwardedFor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest
            {
                Email = "forwarded-headers@example.com",
                Password = "WrongPassword"
            })
        };
        request.Headers.TryAddWithoutValidation(RemoteIpHeader, remoteIp);
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        return await _client.SendAsync(request);
    }

    public sealed class Factory : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
                services.AddSingleton<IStartupFilter, TestConnectionRemoteIpFilter>());
        }
    }

    private sealed class TestConnectionRemoteIpFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    if (context.Request.Headers.TryGetValue(RemoteIpHeader, out var raw)
                        && IPAddress.TryParse(raw.ToString(), out var ip))
                    {
                        context.Connection.RemoteIpAddress = ip;
                    }

                    await nextMiddleware(context);
                });
                next(app);
            };
        }
    }
}
