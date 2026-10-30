using Backend.Configuration;
using Backend.Context;
using Backend.DTOs.People;
using Backend.Entities;
using Backend.Helpers;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Bulk email, SMS, and WhatsApp use the same paid-send guard as login codes.
/// A retry reserves another slot. A blocked retry does not call the provider.
/// </summary>
public class BulkSendCapTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<MainDbContext> _options;
    private readonly Guid _electionId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _personId = Guid.NewGuid();
    private const string Phone = "+14168972901";
    private const string Email = "ada@example.com";

    public BulkSendCapTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bulk-send-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<MainDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        using var db = new MainDbContext(_options);
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        db.Elections.Add(new Election
        {
            ElectionGuid = _electionId,
            Name = "Announcement",
            ElectionType = "LSA",
            NumberToElect = 1,
            SmsText = "Hello {FirstName}",
            EmailText = "Hello {FirstName}",
            RowVersion = new byte[8]
        });
        db.People.Add(new Person
        {
            PersonGuid = _personId,
            ElectionGuid = _electionId,
            LastName = "Voter",
            FirstName = "Ada",
            Phone = Phone,
            Email = Email,
            RowVersion = new byte[8]
        });
        db.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = Phone,
            VoterIdType = "P",
            WhatsAppStatus = "OK"
        });
        db.JoinElectionUsers.Add(new JoinElectionUser
        {
            ElectionGuid = _electionId,
            UserId = _ownerId,
            Role = "Admin"
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Email_SendsWithoutApproval_AndDoesNotConsumeTheCap()
    {
        var harness = Create();

        var started = await harness.Announcements.StartAsync(
            _electionId, AnnouncementChannels.Email, [_personId]);
        var done = await WaitUntilIdle(harness.Announcements, _electionId, AnnouncementChannels.Email);

        Assert.Equal(1, started.Queued);
        Assert.Equal(1, done.Sent);
        Assert.Equal(0, done.Failed);
        harness.Email.Verify(
            sender => sender.SendAsync(Email, It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(0, await PaidSendsUsedAsync());
        Assert.Equal("sent", await LogOutcomeAsync(AnnouncementChannels.Email));
    }

    [Fact]
    public async Task Sms_DoesNotCallTwilio_WhenTheOwnerIsNotApproved()
    {
        var harness = Create();

        await harness.Announcements.StartAsync(_electionId, AnnouncementChannels.Sms, [_personId]);
        var done = await WaitUntilIdle(harness.Announcements, _electionId, AnnouncementChannels.Sms);

        Assert.Equal(WhatsAppNotifyOutcome.Failed, done.Results[0].Outcome);
        Assert.Equal(CodeSendBlockReason.NotApproved, done.Results[0].Error);
        harness.Sms.Verify(
            sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal(0, await PaidSendsUsedAsync());
        var log = await SingleLogAsync();
        Assert.Equal("blocked-not-approved", log.Outcome);
        Assert.DoesNotContain(Phone, log.MaskedDestination);
        Assert.Contains('*', log.MaskedDestination);
        Assert.Contains(harness.Logger.Messages, message =>
            message.Contains(log.MaskedDestination, StringComparison.Ordinal) &&
            !message.Contains(Phone, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SmsRetry_CountsAgainstTheCap_AndDoesNotCallTwilioWhenTheCapIsSpent()
    {
        await ApproveOwnerAsync();
        await SetAllowanceAsync(1);
        var calls = 0;
        var harness = Create();
        harness.Sms
            .Setup(sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((string _, string _, Guid _, Guid _, CancellationToken _) =>
            {
                calls++;
                var result = calls == 1
                    ? AnnouncementProviderResult.Failed("The operation has timed out.")
                    : AnnouncementProviderResult.Succeeded();
                return Task.FromResult(result);
            });

        await harness.Announcements.StartAsync(_electionId, AnnouncementChannels.Sms, [_personId]);
        var failed = await WaitUntilIdle(harness.Announcements, _electionId, AnnouncementChannels.Sms);

        Assert.Equal(1, calls);
        Assert.Equal(1, await PaidSendsUsedAsync());
        Assert.Equal("The operation has timed out.", failed.Results[0].Error);
        Assert.Equal("send-failed", await LogOutcomeAsync(AnnouncementChannels.Sms));
        var failedLog = await SingleLogAsync(AnnouncementChannels.Sms, "send-failed");
        Assert.DoesNotContain(Phone, failedLog.MaskedDestination);

        await harness.Announcements.RetryFailedAsync(_electionId, AnnouncementChannels.Sms);
        var blocked = await WaitUntilIdle(harness.Announcements, _electionId, AnnouncementChannels.Sms);

        Assert.Equal(1, calls);
        Assert.Equal(1, await PaidSendsUsedAsync());
        Assert.Equal(CodeSendBlockReason.ElectionCap, blocked.Results[0].Error);
        harness.Sms.Verify(
            sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once);

        await SetAllowanceAsync(2);
        await harness.Announcements.RetryFailedAsync(_electionId, AnnouncementChannels.Sms);
        var sent = await WaitUntilIdle(harness.Announcements, _electionId, AnnouncementChannels.Sms);

        Assert.Equal(2, calls);
        Assert.Equal(2, await PaidSendsUsedAsync());
        Assert.Equal(WhatsAppNotifyOutcome.Sent, sent.Results[0].Outcome);
    }

    [Fact]
    public async Task Email_DoesNotSend_WhenTheElectionIsFrozen()
    {
        await using (var db = new MainDbContext(_options))
        {
            db.ElectionSendControls.Add(new ElectionSendControl
            {
                ElectionGuid = _electionId,
                SendsFrozen = true
            });
            await db.SaveChangesAsync();
        }

        var harness = Create();
        await harness.Announcements.StartAsync(_electionId, AnnouncementChannels.Email, [_personId]);
        var done = await WaitUntilIdle(harness.Announcements, _electionId, AnnouncementChannels.Email);

        Assert.Equal(CodeSendBlockReason.Frozen, done.Results[0].Error);
        harness.Email.Verify(
            sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal(0, await PaidSendsUsedAsync());
    }

    [Fact]
    public async Task Sms_DoesNotSend_WhenTheElectionIsFlagged()
    {
        await ApproveOwnerAsync();
        await using (var db = new MainDbContext(_options))
        {
            db.ElectionSendControls.Add(new ElectionSendControl
            {
                ElectionGuid = _electionId,
                Flagged = true,
                AllowanceOverride = 25
            });
            await db.SaveChangesAsync();
        }

        var harness = Create();
        await harness.Announcements.StartAsync(_electionId, AnnouncementChannels.Sms, [_personId]);
        var done = await WaitUntilIdle(harness.Announcements, _electionId, AnnouncementChannels.Sms);

        Assert.Equal(CodeSendBlockReason.Flagged, done.Results[0].Error);
        harness.Sms.Verify(
            sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal(0, await PaidSendsUsedAsync());
    }

    [Fact]
    public async Task WhatsAppRetry_CountsAgainstTheCap_AndDoesNotCallGreenApiWhenTheCapIsSpent()
    {
        await ApproveOwnerAsync();
        await SetAllowanceAsync(1);
        var calls = 0;
        var harness = Create();
        harness.WhatsAppClient
            .Setup(client => client.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string _, string _, CancellationToken _) =>
            {
                calls++;
                var result = calls == 1
                    ? GreenApiWhatsAppSendResult.Failed("The operation has timed out.")
                    : GreenApiWhatsAppSendResult.Succeeded("wa-id");
                return Task.FromResult(result);
            });

        await harness.WhatsApp.StartAsync(_electionId, [_personId]);
        var failed = await WaitUntilIdle(harness.WhatsApp, _electionId);

        Assert.Equal(1, calls);
        Assert.Equal(1, await PaidSendsUsedAsync());
        Assert.Equal("The operation has timed out.", failed.Results[0].Error);
        Assert.Equal(0, await CountSmsLogsAsync());

        await harness.WhatsApp.RetryFailedAsync(_electionId);
        var blocked = await WaitUntilIdle(harness.WhatsApp, _electionId);

        Assert.Equal(1, calls);
        Assert.Equal(CodeSendBlockReason.ElectionCap, blocked.Results[0].Error);
        Assert.Equal(1, await PaidSendsUsedAsync());

        await SetAllowanceAsync(2);
        await harness.WhatsApp.RetryFailedAsync(_electionId);
        var sent = await WaitUntilIdle(harness.WhatsApp, _electionId);

        Assert.Equal(2, calls);
        Assert.Equal(2, await PaidSendsUsedAsync());
        Assert.Equal(WhatsAppNotifyOutcome.Sent, sent.Results[0].Outcome);
        Assert.Equal(1, await CountSmsLogsAsync());
    }

    [Fact]
    public async Task Sms_SecondNumberOnAHotPrefix_DoesNotCallTwilio()
    {
        await ApproveOwnerAsync();
        var secondId = Guid.NewGuid();
        const string secondPhone = "+14168972902";
        await using (var db = new MainDbContext(_options))
        {
            db.People.Add(new Person
            {
                PersonGuid = secondId,
                ElectionGuid = _electionId,
                LastName = "Voter",
                FirstName = "Bea",
                Phone = secondPhone,
                RowVersion = new byte[8]
            });
            await db.SaveChangesAsync();
        }

        var harness = Create(new AntiAbuseOptions
        {
            ElectionPaidSendAllowance = 25,
            OwnerDailyPaidSendCap = 100,
            PhonePrefixSendLimit = 1,
            PhonePrefixDigits = 6,
            PhonePrefixWindowMinutes = 60
        });

        await harness.Announcements.StartAsync(_electionId, AnnouncementChannels.Sms, [_personId, secondId]);
        var done = await WaitUntilIdle(harness.Announcements, _electionId, AnnouncementChannels.Sms);

        Assert.Equal(1, done.Sent);
        Assert.Equal(1, done.Failed);
        Assert.Equal(CodeSendBlockReason.PrefixLimit, done.Results[1].Error);
        harness.Sms.Verify(
            sender => sender.SendAsync(secondPhone, It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        harness.Sms.Verify(
            sender => sender.SendAsync(Phone, It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // Another connection may still be closing the file.
        }
    }

    private Harness Create(AntiAbuseOptions? options = null)
    {
        options ??= new AntiAbuseOptions
        {
            ElectionPaidSendAllowance = 25,
            OwnerDailyPaidSendCap = 100
        };
        var alerts = new Mock<IAbuseAlertService>();
        var sms = new Mock<IAnnouncementSmsSender>();
        sms.Setup(sender => sender.IsConfigured()).Returns(true);
        sms.Setup(sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AnnouncementProviderResult.Succeeded());
        var email = new Mock<IAnnouncementEmailSender>();
        email.Setup(sender => sender.IsConfigured()).Returns(true);
        email.Setup(sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AnnouncementProviderResult.Succeeded());
        var whatsApp = new Mock<IGreenApiWhatsAppClient>();
        whatsApp.Setup(client => client.IsConfigured()).Returns(true);
        whatsApp.Setup(client => client.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GreenApiWhatsAppSendResult.Succeeded("wa-id"));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<MainDbContext>(_ => new MainDbContext(_options));
        services.AddScoped<PaidSendCounters>();
        services.AddSingleton(alerts.Object);
        services.AddSingleton<IAbuseAlertService>(alerts.Object);
        services.AddSingleton<ICodeSendClock, SystemCodeSendClock>();
        services.AddSingleton(Options.Create(options));
        services.AddScoped<IPhonePrefixSendLimiter, PhonePrefixSendLimiter>();
        services.AddScoped<ICodeSendGuard, CodeSendGuard>();
        services.AddSingleton(sms.Object);
        services.AddSingleton<IAnnouncementSmsSender>(sms.Object);
        services.AddSingleton(email.Object);
        services.AddSingleton<IAnnouncementEmailSender>(email.Object);
        services.AddSingleton(whatsApp.Object);
        services.AddSingleton<IGreenApiWhatsAppClient>(whatsApp.Object);
        var provider = services.BuildServiceProvider();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ClientEnv:frontendUrl"] = "https://example.test",
                ["Email:FromAddress"] = "noreply@example.test",
                ["Email:FromName"] = "TallyJ"
            })
            .Build();
        var logger = new CollectingLogger<AnnouncementQueue>();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        return new Harness(
            new AnnouncementQueue(scopeFactory, configuration, logger, (_, _) => Task.CompletedTask),
            new WhatsAppNotifyQueue(scopeFactory, configuration, new CollectingLogger<WhatsAppNotifyQueue>(), _ => Task.CompletedTask),
            sms,
            email,
            whatsApp,
            logger);
    }

    private async Task ApproveOwnerAsync()
    {
        await using var db = new MainDbContext(_options);
        db.OwnerSendControls.Add(new OwnerSendControl
        {
            UserId = _ownerId,
            PaidSendsApproved = true
        });
        await db.SaveChangesAsync();
    }

    private async Task SetAllowanceAsync(int allowance)
    {
        await using var db = new MainDbContext(_options);
        var row = await db.ElectionSendControls.SingleOrDefaultAsync(item => item.ElectionGuid == _electionId);
        if (row == null)
        {
            db.ElectionSendControls.Add(new ElectionSendControl
            {
                ElectionGuid = _electionId,
                AllowanceOverride = allowance
            });
        }
        else
        {
            row.AllowanceOverride = allowance;
        }

        await db.SaveChangesAsync();
    }

    private async Task<int> PaidSendsUsedAsync()
    {
        await using var db = new MainDbContext(_options);
        return await db.ElectionSendControls
            .Where(row => row.ElectionGuid == _electionId)
            .Select(row => row.PaidSendsUsed)
            .SingleOrDefaultAsync();
    }

    private async Task<string?> LogOutcomeAsync(string channel)
    {
        await using var db = new MainDbContext(_options);
        return await db.CodeSendLogs
            .Where(row => row.Channel == channel)
            .OrderByDescending(row => row.RowId)
            .Select(row => row.Outcome)
            .FirstOrDefaultAsync();
    }

    private async Task<CodeSendLog> SingleLogAsync(string? channel = null, string? outcome = null)
    {
        await using var db = new MainDbContext(_options);
        var query = db.CodeSendLogs.AsNoTracking().AsQueryable();
        if (channel != null)
        {
            query = query.Where(row => row.Channel == channel);
        }

        if (outcome != null)
        {
            query = query.Where(row => row.Outcome == outcome);
        }

        return await query.SingleAsync();
    }

    private async Task<int> CountSmsLogsAsync()
    {
        await using var db = new MainDbContext(_options);
        return await db.SmsLogs.CountAsync();
    }

    private static async Task<AnnouncementStatusDto> WaitUntilIdle(
        IAnnouncementQueue queue,
        Guid electionGuid,
        string channel)
    {
        for (var i = 0; i < 200; i++)
        {
            var status = queue.GetStatus(electionGuid, channel);
            if (status is not { Running: true })
            {
                return status ?? throw new InvalidOperationException("missing announcement status");
            }

            await Task.Delay(10);
        }

        throw new TimeoutException("announcement queue still running");
    }

    private static async Task<WhatsAppNotifyStatusDto> WaitUntilIdle(
        IWhatsAppNotifyQueue queue,
        Guid electionGuid)
    {
        for (var i = 0; i < 200; i++)
        {
            var status = queue.GetStatus(electionGuid);
            if (status is not { Running: true })
            {
                return status ?? throw new InvalidOperationException("missing notify status");
            }

            await Task.Delay(10);
        }

        throw new TimeoutException("WhatsApp notify queue still running");
    }

    private sealed record Harness(
        AnnouncementQueue Announcements,
        WhatsAppNotifyQueue WhatsApp,
        Mock<IAnnouncementSmsSender> Sms,
        Mock<IAnnouncementEmailSender> Email,
        Mock<IGreenApiWhatsAppClient> WhatsAppClient,
        CollectingLogger<AnnouncementQueue> Logger);

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
