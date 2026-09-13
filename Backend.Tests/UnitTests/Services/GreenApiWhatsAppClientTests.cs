using System.Net;
using Backend.Helpers;
using Backend.Services.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// GreenAPI checkWhatsapp HTTP helper. Tests mock HTTP — no live account.
/// </summary>
public class GreenApiWhatsAppClientTests
{
    private const string ValidPhone = "+14168972671";

    [Fact]
    public async Task CheckWhatsAppAsync_NotConfigured_DoesNotCreateHttpClient()
    {
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var client = CreateClient(httpFactory.Object);

        var result = await client.CheckWhatsAppAsync(ValidPhone);

        Assert.False(result.ProviderCalled);
        Assert.Equal(OnlineVoterWhatsAppStatus.CheckFailed, result.Status);
        httpFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CheckWhatsAppAsync_ExistsTrue_ReturnsOk()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"existsWhatsapp":true}""");
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient("GreenApi")).Returns(new HttpClient(handler));
        var client = CreateClient(httpFactory.Object, Configured());

        var result = await client.CheckWhatsAppAsync(ValidPhone);

        Assert.True(result.ProviderCalled);
        Assert.Equal(OnlineVoterWhatsAppStatus.Ok, result.Status);
        Assert.Equal(
            "https://api.green-api.com/waInstance1234/checkWhatsapp/token",
            handler.LastRequest?.RequestUri?.ToString());
        Assert.Contains("14168972671", handler.LastBody);
    }

    [Fact]
    public async Task CheckWhatsAppAsync_ExistsFalse_ReturnsNoWa()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"existsWhatsapp":false}""");
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient("GreenApi")).Returns(new HttpClient(handler));
        var client = CreateClient(httpFactory.Object, Configured());

        var result = await client.CheckWhatsAppAsync(ValidPhone);

        Assert.True(result.ProviderCalled);
        Assert.Equal(OnlineVoterWhatsAppStatus.NoWa, result.Status);
    }

    [Fact]
    public async Task CheckWhatsAppAsync_Cancelled_RethrowsWithoutMappingToCheckFailed()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"existsWhatsapp":true}""")
        {
            ThrowCanceled = true
        };
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient("GreenApi")).Returns(new HttpClient(handler));
        var client = CreateClient(httpFactory.Object, Configured());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.CheckWhatsAppAsync(ValidPhone, cts.Token));
    }

    [Fact]
    public async Task SendMessageAsync_NotConfigured_DoesNotCreateHttpClient()
    {
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var client = CreateClient(httpFactory.Object);

        var result = await client.SendMessageAsync(ValidPhone, "Hello");

        Assert.False(result.ProviderCalled);
        Assert.False(result.Sent);
        httpFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_Configured_PostsSendMessage()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"idMessage":"true_abc"}""");
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient("GreenApi")).Returns(new HttpClient(handler));
        var client = CreateClient(httpFactory.Object, Configured());

        var result = await client.SendMessageAsync(ValidPhone, "Hello {FirstName}");

        Assert.True(result.ProviderCalled);
        Assert.True(result.Sent);
        Assert.Equal("true_abc", result.MessageId);
        Assert.Equal(
            "https://api.green-api.com/waInstance1234/sendMessage/token",
            handler.LastRequest?.RequestUri?.ToString());
        Assert.Contains("14168972671@c.us", handler.LastBody);
        Assert.Contains("Hello {FirstName}", handler.LastBody);
    }

    [Fact]
    public void IsConfigured_MatchesHelper()
    {
        Assert.False(CreateClient(Mock.Of<IHttpClientFactory>()).IsConfigured());
        Assert.True(CreateClient(Mock.Of<IHttpClientFactory>(), Configured()).IsConfigured());
    }

    [Fact]
    public async Task CheckWhatsAppAsync_HttpFailure_ReturnsCheckFailed()
    {
        var handler = new StubHandler(HttpStatusCode.BadRequest, """{"error":true}""");
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient("GreenApi")).Returns(new HttpClient(handler));
        var client = CreateClient(httpFactory.Object, Configured());

        var result = await client.CheckWhatsAppAsync(ValidPhone);

        Assert.True(result.ProviderCalled);
        Assert.Equal(OnlineVoterWhatsAppStatus.CheckFailed, result.Status);
    }

    private static GreenApiWhatsAppClient CreateClient(
        IHttpClientFactory httpFactory,
        Dictionary<string, string?>? values = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?>())
            .Build();

        return new GreenApiWhatsAppClient(
            configuration,
            httpFactory,
            Mock.Of<ILogger<GreenApiWhatsAppClient>>());
    }

    private static Dictionary<string, string?> Configured() => new()
    {
        ["GreenApi:IdInstance"] = "1234",
        ["GreenApi:ApiToken"] = "token",
        ["GreenApi:BaseUrl"] = "https://api.green-api.com"
    };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public StubHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        public string LastBody { get; private set; } = "";

        public bool ThrowCanceled { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (ThrowCanceled)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (request.Content != null)
            {
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody)
            };
        }
    }
}
