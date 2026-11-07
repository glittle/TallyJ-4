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
        MimeMessage? sent = null;
        var email = new Mock<IEmailSender>();
        email.Setup(sender => sender.SendAsync(It.IsAny<MimeMessage>()))
            .Callback<MimeMessage>(message => sent = message)
            .Returns(Task.CompletedTask);
        var sender = CreateSender(email.Object, out var logger);

        var result = await sender.SendAsync(
            "ada@example.com",
            "teller@congregation.example",
            "Local Tellers",
            "Ballot notice",
            "Hello",
            CancellationToken.None);

        Assert.True(result.Sent);
        Assert.NotNull(sent);
        Assert.Equal("noreply@tallyj.com", ((MailboxAddress)sent!.From.Single()).Address);
        Assert.Equal("Local Tellers", ((MailboxAddress)sent.From.Single()).Name);
        Assert.Equal("teller@congregation.example", ((MailboxAddress)sent.ReplyTo.Single()).Address);
        Assert.Equal("Ballot notice", sent.Subject);
        Assert.Empty(logger.Messages);
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

    private static AnnouncementEmailSender CreateSender(IEmailSender email, out ListLogger logger)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:SmtpHost"] = "smtp.example.com",
            ["Email:FromAddress"] = "noreply@tallyj.com",
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
