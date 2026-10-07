using Backend.Authorization;
using Backend.Configuration;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Cap and first-send alerts email the super admin once per throttle window.
/// </summary>
public class AbuseAlertServiceTests : ServiceTestBase
{
    private readonly Mock<IEmailSender> _email = new();
    private readonly Mock<ISentryWarningCapture> _sentry = new();

    public AbuseAlertServiceTests()
    {
        _email.Setup(sender => sender.SendAsync(It.IsAny<MimeMessage>())).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CapHit_EmailsOnce_UntilTheThrottleWindowPasses()
    {
        var service = CreateService(throttleHours: 24);
        var alert = new AbuseCapHitAlert(
            "cap:election:abc",
            "election",
            Guid.NewGuid(),
            "Town election",
            null,
            25,
            25);

        await service.NotifyCapHitAsync(alert);
        await service.NotifyCapHitAsync(alert);

        _email.Verify(sender => sender.SendAsync(It.IsAny<MimeMessage>()), Times.Once);
        _sentry.Verify(capture => capture.Capture(
            It.Is<string>(subject => subject.Contains("cap")),
            It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Once);
        Assert.Equal(1, await Context.AbuseAlertStates.CountAsync());
    }

    [Fact]
    public async Task FirstPaidSend_UsesTheConfiguredAlertAddress()
    {
        MimeMessage? sent = null;
        _email.Setup(sender => sender.SendAsync(It.IsAny<MimeMessage>()))
            .Callback<MimeMessage>(message => sent = message)
            .Returns(Task.CompletedTask);

        var service = CreateService(throttleHours: 24, alertEmails: new[] { "glen@example.com" });
        await service.NotifyFirstPaidSendAsync(new AbuseFirstPaidSendAlert(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Town election",
            "sms",
            "+14*****00"));

        Assert.NotNull(sent);
        Assert.Contains(sent!.To.Mailboxes, box => box.Address == "glen@example.com");
        Assert.Contains("+14*****00", sent.TextBody);
        Assert.DoesNotContain("+14165550100", sent.TextBody);
    }

    private AbuseAlertService CreateService(int throttleHours, string[]? alertEmails = null)
    {
        return new AbuseAlertService(
            Context,
            _email.Object,
            _sentry.Object,
            NullLogger<AbuseAlertService>.Instance,
            Options.Create(new AntiAbuseOptions
            {
                AlertThrottleHours = throttleHours,
                AlertEmails = alertEmails ?? Array.Empty<string>()
            }),
            Options.Create(new SuperAdminSettings
            {
                Emails = new[] { "admin@tallyj.test" }
            }));
    }
}
