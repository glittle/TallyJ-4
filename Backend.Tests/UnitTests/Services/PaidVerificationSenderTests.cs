using System.Net;
using Backend.Services.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Defense-in-depth: the sender itself must not call Twilio/GreenAPI for rejected destinations.
/// </summary>
public class PaidVerificationSenderTests
{
    [Theory]
    [InlineData("+15551234567")]
    [InlineData("+14155550100")]
    [InlineData("not-a-phone")]
    public async Task SendSms_RejectedDestination_DoesNotCreateHttpClient(string phone)
    {
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var sender = CreateSender(httpFactory.Object);

        var sent = await sender.SendSmsAsync(phone, "ABC123");

        Assert.False(sent);
        httpFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendVoice_ReservedPhone_DoesNotCreateHttpClient()
    {
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var sender = CreateSender(httpFactory.Object);

        var sent = await sender.SendVoiceAsync("+15550123456", "ABC123");

        Assert.False(sent);
        httpFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendWhatsApp_ReservedPhone_DoesNotCreateHttpClient()
    {
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var sender = CreateSender(httpFactory.Object);

        var sent = await sender.SendWhatsAppAsync("+14155550100", "ABC123");

        Assert.False(sent);
        httpFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendSms_ValidPhone_WithoutTwilioConfig_SkipsWithoutHttp()
    {
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var sender = CreateSender(httpFactory.Object);

        var sent = await sender.SendSmsAsync("+14168972671", "ABC123");

        Assert.True(sent);
        httpFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendSms_ValidPhone_WithTwilioConfig_PostsToTwilio()
    {
        var handler = new RecordingHandler(HttpStatusCode.Created);
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler));

        var sender = CreateSender(httpFactory.Object, new Dictionary<string, string?>
        {
            ["Twilio:AccountSid"] = "ACtest",
            ["Twilio:AuthToken"] = "token",
            ["Twilio:FromNumber"] = "+14165550000"
        });

        var sent = await sender.SendSmsAsync("+14168972671", "ABC123");

        Assert.True(sent);
        Assert.NotNull(handler.LastRequest);
        Assert.Contains("api.twilio.com", handler.LastRequest!.RequestUri!.Host, StringComparison.Ordinal);
        Assert.Contains("/Messages.json", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
    }

    private static PaidVerificationSender CreateSender(
        IHttpClientFactory httpFactory,
        Dictionary<string, string?>? values = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?>())
            .Build();

        return new PaidVerificationSender(
            configuration,
            httpFactory,
            Mock.Of<ILogger<PaidVerificationSender>>());
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public RecordingHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent("{}")
            });
        }
    }
}
