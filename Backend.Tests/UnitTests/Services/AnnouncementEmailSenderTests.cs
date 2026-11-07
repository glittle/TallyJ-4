using Backend.Services;
using Backend.Services.Auth;
using MailKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;

namespace Backend.Tests.UnitTests.Services;

public class AnnouncementEmailSenderTests
{
    [Fact]
    public async Task Send_UsesSystemFrom_AndElectionAddressAsReplyTo()
    {
        var sent = await CaptureAsync(
            "teller@congregation.example",
            "Local Tellers",
            "Ballot notice");

        Assert.Equal("noreply@tallyj.com", FromAddress(sent));
        Assert.Equal("Local Tellers", FromName(sent));
        Assert.Equal("teller@congregation.example", ReplyAddress(sent));
        Assert.Equal("Local Tellers", ReplyName(sent));
        Assert.Equal("Ballot notice", sent.Subject);
        Assert.DoesNotContain("teller@congregation.example", FromAddress(sent), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_OmitsReplyTo_WhenTheElectionAddressIsMissing()
    {
        var sent = await CaptureAsync(null, null, "Hello");

        Assert.Equal("noreply@tallyj.com", FromAddress(sent));
        Assert.Equal("TallyJ", FromName(sent));
        Assert.Empty(sent.ReplyTo);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("<not-configured>")]
    public async Task Send_OmitsReplyTo_WhenTheElectionAddressIsInvalid(string replyTo)
    {
        var sent = await CaptureAsync(replyTo, "Local Tellers", "Hello");

        Assert.Equal("noreply@tallyj.com", FromAddress(sent));
        Assert.Equal("Local Tellers", FromName(sent));
        Assert.Empty(sent.ReplyTo);
        Assert.DoesNotContain(replyTo, sent.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_DoesNotInventAFromAddress_WhenTheSystemSenderIsMissing()
    {
        var email = new Mock<IEmailSender>();
        var sender = CreateSender(email.Object, out _, fromAddress: null);

        var result = await sender.SendAsync(
            "ada@example.com",
            "teller@congregation.example",
            "Local Tellers",
            "Hello",
            "Body",
            CancellationToken.None);

        Assert.False(result.Sent);
        Assert.Equal("not-configured", result.Error);
        email.Verify(item => item.SendAsync(It.IsAny<MimeMessage>()), Times.Never);
    }

    [Fact]
    public async Task Send_LogsExceptionTypeAndSmtpStatus_NotTheRecipient()
    {
        var email = new Mock<IEmailSender>();
        email.Setup(sender => sender.SendAsync(It.IsAny<MimeMessage>()))
            .ThrowsAsync(new SmtpCommandException(
                SmtpErrorCode.RecipientNotAccepted,
                SmtpStatusCode.MailboxUnavailable,
                "5.1.1 <ada@example.com>: Recipient address rejected"));
        var sender = CreateSender(email.Object, out var logger);

        var result = await sender.SendAsync(
            "ada@example.com",
            null,
            null,
            "Hello",
            "Body",
            CancellationToken.None);

        Assert.False(result.Sent);
        Assert.DoesNotContain("ada@example.com", result.Error ?? "", StringComparison.Ordinal);
        var message = Assert.Single(logger.Messages);
        Assert.Contains(nameof(SmtpCommandException), message, StringComparison.Ordinal);
        Assert.Contains(((int)SmtpStatusCode.MailboxUnavailable).ToString(), message, StringComparison.Ordinal);
        Assert.DoesNotContain("ada@example.com", message, StringComparison.Ordinal);
        Assert.DoesNotContain("Recipient address rejected", message, StringComparison.Ordinal);
    }

    private static async Task<MimeMessage> CaptureAsync(string? replyTo, string? displayName, string subject)
    {
        MimeMessage? sent = null;
        var email = new Mock<IEmailSender>();
        email.Setup(sender => sender.SendAsync(It.IsAny<MimeMessage>()))
            .Callback<MimeMessage>(message => sent = message)
            .Returns(Task.CompletedTask);
        var sender = CreateSender(email.Object, out _);
        var result = await sender.SendAsync(
            "ada@example.com",
            replyTo,
            displayName,
            subject,
            "Hello",
            CancellationToken.None);
        Assert.True(result.Sent);
        Assert.NotNull(sent);
        return sent!;
    }

    private static string FromAddress(MimeMessage message) =>
        ((MailboxAddress)message.From.Single()).Address ?? "";

    private static string FromName(MimeMessage message) =>
        ((MailboxAddress)message.From.Single()).Name ?? "";

    private static string ReplyAddress(MimeMessage message) =>
        ((MailboxAddress)message.ReplyTo.Single()).Address ?? "";

    private static string ReplyName(MimeMessage message) =>
        ((MailboxAddress)message.ReplyTo.Single()).Name ?? "";

    private static AnnouncementEmailSender CreateSender(
        IEmailSender email,
        out ListLogger logger,
        string? fromAddress = "noreply@tallyj.com")
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:SmtpHost"] = "smtp.example.com",
            ["Email:FromAddress"] = fromAddress,
            ["Email:FromName"] = "TallyJ"
        }).Build();
        logger = new ListLogger();
        return new AnnouncementEmailSender(configuration, email, logger);
    }

    private sealed class ListLogger : ILogger<AnnouncementEmailSender>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Assert.Null(exception);
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
