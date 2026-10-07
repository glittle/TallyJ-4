using Backend.Authorization;
using Backend.Configuration;
using Backend.Context;
using Backend.Entities;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        Assert.Equal("TallyJ alerts", sent!.From.Mailboxes.Single().Name);
        Assert.Equal("alerts@tallyj.test", sent.From.Mailboxes.Single().Address);
        Assert.Contains(sent.To.Mailboxes, box => box.Address == "glen@example.com");
        Assert.Contains("+14*****00", sent.TextBody);
        Assert.DoesNotContain("+14165550100", sent.TextBody);
    }

    [Fact]
    public async Task MalformedRecipient_IsSkipped_AndAValidRecipientStillReceivesTheAlert()
    {
        MimeMessage? sent = null;
        _email.Setup(sender => sender.SendAsync(It.IsAny<MimeMessage>()))
            .Callback<MimeMessage>(message => sent = message)
            .Returns(Task.CompletedTask);

        var service = CreateService(throttleHours: 24, alertEmails: new[] { "not an email", "glen@example.com" });
        await service.NotifyCapHitAsync(SampleCapAlert());

        Assert.NotNull(sent);
        Assert.Equal("glen@example.com", sent!.To.Mailboxes.Single().Address);
    }

    [Fact]
    public async Task EmailFailure_DoesNotThrow()
    {
        _email.Setup(sender => sender.SendAsync(It.IsAny<MimeMessage>()))
            .ThrowsAsync(new InvalidOperationException("smtp refused the message"));

        var service = CreateService(throttleHours: 24, alertEmails: new[] { "glen@example.com" });
        await service.NotifyCapHitAsync(SampleCapAlert());

        Assert.Equal(1, await Context.AbuseAlertStates.CountAsync());
    }

    [Fact]
    public async Task LostThrottleInsert_IsDetached_SoALaterCodeSendLogSaveSucceeds()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"alert-race-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseSqlite($"Data Source={dbPath};Cache=Shared")
            .Options;

        try
        {
            await using (var setup = new MainDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                await setup.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;");
            }

            await using var firstContext = new MainDbContext(options);
            await using var secondContext = new MainDbContext(options);
            await firstContext.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");
            await secondContext.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");

            var bothPastTheLookup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var arrived = 0;
            _email.Setup(sender => sender.SendAsync(It.IsAny<MimeMessage>()))
                .Returns(async () =>
                {
                    if (Interlocked.Increment(ref arrived) == 2)
                    {
                        bothPastTheLookup.TrySetResult();
                    }

                    await bothPastTheLookup.Task.WaitAsync(TimeSpan.FromSeconds(5));
                });

            var first = CreateService(firstContext, alertEmails: new[] { "glen@example.com" });
            var second = CreateService(secondContext, alertEmails: new[] { "glen@example.com" });
            var alert = SampleCapAlert("cap:election:race");
            await Task.WhenAll(first.NotifyCapHitAsync(alert), second.NotifyCapHitAsync(alert));

            await SaveCodeSendLogAsync(firstContext);
            await SaveCodeSendLogAsync(secondContext);

            Assert.Equal(2, await firstContext.CodeSendLogs.CountAsync());
            Assert.Equal(1, await firstContext.AbuseAlertStates.CountAsync());
            Assert.DoesNotContain(
                firstContext.ChangeTracker.Entries<AbuseAlertState>(),
                entry => entry.State == EntityState.Added);
            Assert.DoesNotContain(
                secondContext.ChangeTracker.Entries<AbuseAlertState>(),
                entry => entry.State == EntityState.Added);
        }
        finally
        {
            try
            {
                File.Delete(dbPath);
            }
            catch (IOException)
            {
                // The SQLite pool may still be closing the file.
            }
        }
    }

    private AbuseAlertService CreateService(int throttleHours, string[]? alertEmails = null)
    {
        return CreateService(Context, throttleHours, alertEmails);
    }

    private AbuseAlertService CreateService(
        MainDbContext context,
        int throttleHours = 24,
        string[]? alertEmails = null)
    {
        return new AbuseAlertService(
            context,
            _email.Object,
            _sentry.Object,
            NullLogger<AbuseAlertService>.Instance,
            EmailConfiguration(),
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

    private static async Task SaveCodeSendLogAsync(MainDbContext context)
    {
        context.CodeSendLogs.Add(new CodeSendLog
        {
            Channel = "sms",
            MaskedDestination = "+14*****00",
            Outcome = "blocked-election-cap",
            SentAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private static IConfiguration EmailConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:FromAddress"] = "alerts@tallyj.test",
                ["Email:FromName"] = "TallyJ alerts"
            })
            .Build();
    }

    private static AbuseCapHitAlert SampleCapAlert(string alertKey = "cap:election:abc")
    {
        return new AbuseCapHitAlert(alertKey, "election", Guid.NewGuid(), "Town election", null, 25, 25);
    }

}
