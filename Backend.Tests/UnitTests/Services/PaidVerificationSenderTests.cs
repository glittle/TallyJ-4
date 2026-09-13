using System.Net;
using Backend.Helpers;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Defense-in-depth: the sender itself must not call Twilio/GreenAPI for rejected destinations.
/// Successful paid sends persist SmsLog and set Twilio StatusCallback.
/// </summary>
public class PaidVerificationSenderTests : ServiceTestBase
{
    private const string ValidPhone = "+14168972671";

    [Theory]
    [InlineData("+15551234567")]
    [InlineData("+14155550100")]
    [InlineData("not-a-phone")]
    public async Task SendSms_RejectedDestination_DoesNotCreateHttpClientOrLog(string phone)
    {
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var sender = CreateSender(httpFactory.Object);

        var sent = await sender.SendSmsAsync(phone, "ABC123");

        Assert.False(sent);
        httpFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        Assert.Empty(await Context.SmsLogs.ToListAsync());
    }

    [Fact]
    public async Task SendVoice_ReservedPhone_DoesNotCreateHttpClient()
    {
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var sender = CreateSender(httpFactory.Object);

        var sent = await sender.SendVoiceAsync("+15550123456", "ABC123");

        Assert.False(sent);
        httpFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        Assert.Empty(await Context.SmsLogs.ToListAsync());
    }

    [Fact]
    public async Task SendWhatsApp_ReservedPhone_DoesNotCreateHttpClient()
    {
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var sender = CreateSender(httpFactory.Object);

        var sent = await sender.SendWhatsAppAsync("+14155550100", "ABC123");

        Assert.False(sent);
        httpFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        Assert.Empty(await Context.SmsLogs.ToListAsync());
    }

    [Fact]
    public async Task SendSms_ValidPhone_WithoutTwilioConfig_SkipsWithoutHttpOrLog()
    {
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var sender = CreateSender(httpFactory.Object);

        var sent = await sender.SendSmsAsync(ValidPhone, "ABC123");

        Assert.True(sent);
        httpFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        Assert.Empty(await Context.SmsLogs.ToListAsync());
    }

    [Fact]
    public async Task SendSms_ValidPhone_WithTwilioConfig_PostsStatusCallbackAndInsertsSmsLog()
    {
        var handler = new RecordingHandler(
            HttpStatusCode.Created,
            """{"sid":"SMsendtest001","status":"queued"}""");
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler));

        var sender = CreateSender(httpFactory.Object, new Dictionary<string, string?>
        {
            ["Twilio:AccountSid"] = "ACtest",
            ["Twilio:AuthToken"] = "token",
            ["Twilio:FromNumber"] = "+14165550000",
            ["ClientEnv:apiUrl"] = "https://api.example.com"
        });

        var sent = await sender.SendSmsAsync(ValidPhone, "ABC123");

        Assert.True(sent);
        Assert.NotNull(handler.LastRequest);
        Assert.Contains("api.twilio.com", handler.LastRequest!.RequestUri!.Host, StringComparison.Ordinal);
        Assert.Contains("/Messages.json", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Equal("https://api.example.com/api/Public/smsStatus", handler.LastForm["StatusCallback"]);

        var log = Assert.Single(await Context.SmsLogs.ToListAsync());
        Assert.Equal("SMsendtest001", log.SmsSid);
        Assert.Equal(ValidPhone, log.Phone);
        Assert.Equal("queued", log.LastStatus);
        Assert.NotEqual(default, log.SentDate);
        Assert.Equal(log.SentDate, log.LastDate);
        Assert.Null(log.ElectionGuid);
        Assert.Null(log.PersonGuid);

        var found = await SmsLogPhoneHelper.FindRecentForPhoneAsync(Context, ValidPhone);
        Assert.Equal("SMsendtest001", Assert.Single(found).SmsSid);
    }

    [Fact]
    public async Task SendSms_TwilioSuccessWithoutCallbackUrl_InsertsLogOmitsStatusCallback()
    {
        var handler = new RecordingHandler(
            HttpStatusCode.Created,
            """{"sid":"SMnocallback","status":"queued"}""");
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

        var sent = await sender.SendSmsAsync(ValidPhone, "ABC123");

        Assert.True(sent);
        Assert.False(handler.LastForm.ContainsKey("StatusCallback"));
        Assert.Equal("SMnocallback", Assert.Single(await Context.SmsLogs.ToListAsync()).SmsSid);
    }

    [Fact]
    public async Task SendSms_TwilioHttpFailure_DoesNotInsertSmsLog()
    {
        var handler = new RecordingHandler(HttpStatusCode.BadRequest, """{"code":21211}""");
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler));

        var sender = CreateSender(httpFactory.Object, TwilioOnly());

        var sent = await sender.SendSmsAsync(ValidPhone, "ABC123");

        Assert.False(sent);
        Assert.Empty(await Context.SmsLogs.ToListAsync());
    }

    [Fact]
    public async Task SendSms_SuccessWithoutSid_DoesNotInsertSmsLog()
    {
        var handler = new RecordingHandler(HttpStatusCode.Created, "{}");
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler));

        var sender = CreateSender(httpFactory.Object, TwilioOnly());

        var sent = await sender.SendSmsAsync(ValidPhone, "ABC123");

        Assert.True(sent);
        Assert.Empty(await Context.SmsLogs.ToListAsync());
    }

    [Fact]
    public async Task SendVoice_ValidPhone_PostsStatusCallbackAndInsertsSmsLog()
    {
        var handler = new RecordingHandler(
            HttpStatusCode.Created,
            """{"sid":"CAvoicetest001","status":"queued"}""");
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler));

        var sender = CreateSender(httpFactory.Object, new Dictionary<string, string?>
        {
            ["Twilio:AccountSid"] = "ACtest",
            ["Twilio:AuthToken"] = "token",
            ["Twilio:FromNumber"] = "+14165550000",
            ["Twilio:StatusCallbackUrl"] = "https://hooks.example.com/api/Public/smsStatus"
        });

        var sent = await sender.SendVoiceAsync(ValidPhone, "ABC123");

        Assert.True(sent);
        Assert.Contains("/Calls.json", handler.LastRequest!.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Equal(
            "https://hooks.example.com/api/Public/smsStatus",
            handler.LastForm["StatusCallback"]);
        var log = Assert.Single(await Context.SmsLogs.ToListAsync());
        Assert.Equal("CAvoicetest001", log.SmsSid);
        Assert.Equal(ValidPhone, log.Phone);
        Assert.Equal("queued", log.LastStatus);
    }

    [Fact]
    public async Task SendWhatsApp_ValidPhone_InsertsSmsLogWithoutStatusCallback()
    {
        var handler = new RecordingHandler(
            HttpStatusCode.OK,
            """{"idMessage":"3EB0GREENAPI001"}""");
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler));

        var sender = CreateSender(httpFactory.Object, new Dictionary<string, string?>
        {
            ["GreenApi:IdInstance"] = "1234",
            ["GreenApi:ApiToken"] = "token",
            ["GreenApi:BaseUrl"] = "https://api.green-api.com",
            ["ClientEnv:apiUrl"] = "https://api.example.com"
        });

        var sent = await sender.SendWhatsAppAsync(ValidPhone, "ABC123");

        Assert.True(sent);
        Assert.False(handler.LastForm.ContainsKey("StatusCallback"));
        var log = Assert.Single(await Context.SmsLogs.ToListAsync());
        Assert.Equal("3EB0GREENAPI001", log.SmsSid);
        Assert.Equal(ValidPhone, log.Phone);
        Assert.Equal(SmsLogSendHelper.DefaultLastStatus, log.LastStatus);
        var found = await SmsLogPhoneHelper.FindRecentForPhoneAsync(Context, ValidPhone);
        Assert.Equal("3EB0GREENAPI001", Assert.Single(found).SmsSid);
    }

    private PaidVerificationSender CreateSender(
        IHttpClientFactory httpFactory,
        Dictionary<string, string?>? values = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?>())
            .Build();

        return new PaidVerificationSender(
            configuration,
            httpFactory,
            Context,
            Mock.Of<ILogger<PaidVerificationSender>>());
    }

    private static Dictionary<string, string?> TwilioOnly() => new()
    {
        ["Twilio:AccountSid"] = "ACtest",
        ["Twilio:AuthToken"] = "token",
        ["Twilio:FromNumber"] = "+14165550000"
    };

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public RecordingHandler(HttpStatusCode statusCode, string responseBody = "{}")
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        public Dictionary<string, string> LastForm { get; private set; } = new(StringComparer.Ordinal);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
            {
                var raw = await request.Content.ReadAsStringAsync(cancellationToken);
                LastForm = ParseForm(raw);
            }

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody)
            };
        }

        private static Dictionary<string, string> ParseForm(string raw)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(raw) || raw.StartsWith('{'))
            {
                return dict;
            }

            foreach (var pair in raw.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                var key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
                var value = parts.Length > 1
                    ? Uri.UnescapeDataString(parts[1].Replace('+', ' '))
                    : "";
                dict[key] = value;
            }

            return dict;
        }
    }
}
