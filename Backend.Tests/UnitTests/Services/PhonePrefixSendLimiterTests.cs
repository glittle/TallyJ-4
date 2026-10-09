using Backend.Configuration;
using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// SMS, voice, and WhatsApp share a sliding per-prefix cap. Email does not.
/// A hit is logged masked and alerts the super admin. The cap slot is not kept
/// when the send is refused.
/// </summary>
public class PhonePrefixSendLimiterTests : IDisposable
{
    private const string TorontoPhone = "+14168972671";
    private const string SamePrefixPhone = "+14168970000";
    private const string OtherPrefixPhone = "+16045550199";

    private readonly string _dbPath;
    private readonly DbContextOptions<MainDbContext> _options;
    private readonly MutableClock _clock = new();
    private readonly Mock<IAbuseAlertService> _alerts = new();
    private readonly Guid _electionId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly AntiAbuseOptions _antiAbuse = new()
    {
        ElectionPaidSendAllowance = 25,
        OwnerDailyPaidSendCap = 100,
        PhonePrefixSendLimit = 2,
        PhonePrefixWindowMinutes = 60,
        PhonePrefixDigits = 6
    };

    public PhonePrefixSendLimiterTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"prefix-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<MainDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        using var context = new MainDbContext(_options);
        context.Database.EnsureCreated();
        context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        context.Elections.Add(new Election
        {
            ElectionGuid = _electionId,
            Name = "Prefix cap election",
            UseOnlineVoting = true,
            OnlineWhenOpen = _clock.UtcNow.AddHours(-1),
            ElectionStage = ElectionStage.GatheringBallots,
            RowVersion = new byte[8]
        });
        context.People.Add(new Person
        {
            ElectionGuid = _electionId,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Voter",
            Phone = TorontoPhone,
            Email = "ada@example.com",
            CanVote = true,
            RowVersion = new byte[8]
        });
        context.JoinElectionUsers.Add(new JoinElectionUser
        {
            ElectionGuid = _electionId,
            UserId = _ownerId,
            Role = "Admin"
        });
        context.OwnerSendControls.Add(new OwnerSendControl
        {
            UserId = _ownerId,
            PaidSendsApproved = true,
            ApprovedAt = _clock.UtcNow
        });
        context.SaveChanges();
    }

    [Fact]
    public async Task SlidingWindow_BlocksThePrefix_ThenAllowsAgainAfterItSlides()
    {
        Assert.True((await ConsumeAsync(TorontoPhone)).Allowed);
        Assert.True((await ConsumeAsync(SamePrefixPhone)).Allowed);
        var blocked = await ConsumeAsync(TorontoPhone);
        Assert.False(blocked.Allowed);
        Assert.Equal(CodeSendBlockReason.PrefixLimit, blocked.BlockReason);
        Assert.Equal("141689", blocked.Prefix);

        _clock.UtcNow = DateTimeOffset.Parse("2026-10-07T13:00:00Z");
        var stillBlocked = await ConsumeAsync(TorontoPhone);
        Assert.False(stillBlocked.Allowed);

        _clock.UtcNow = DateTimeOffset.Parse("2026-10-07T14:00:00Z");
        Assert.True((await ConsumeAsync(TorontoPhone)).Allowed);
        Assert.True((await ConsumeAsync(OtherPrefixPhone)).Allowed);
    }

    [Fact]
    public async Task Email_IsNotBlocked_AndIsNotCounted()
    {
        Assert.True((await ConsumeAsync(TorontoPhone)).Allowed);
        Assert.True((await ConsumeAsync(SamePrefixPhone)).Allowed);

        var email = await ConsumeAsync("ada@example.com", "email");
        Assert.True(email.Allowed);
        Assert.Null(email.BlockReason);

        await using var context = new MainDbContext(_options);
        var row = await context.PhonePrefixSendCounters.SingleAsync();
        Assert.Equal(2, row.SendCount);
        Assert.Equal("141689", row.Prefix);
    }

    [Fact]
    public async Task Guard_BlocksLogsAndAlerts_AndRefundsThePaidCaps()
    {
        _antiAbuse.PhonePrefixSendLimit = 1;

        var first = await ReserveAsync("sms", TorontoPhone);
        var second = await ReserveAsync("sms", SamePrefixPhone);
        var email = await ReserveAsync("email", "ada@example.com");

        Assert.True(first.Allowed);
        Assert.False(second.Allowed);
        Assert.Equal(CodeSendBlockReason.PrefixLimit, second.BlockReason);
        Assert.True(email.Allowed);

        await using var context = new MainDbContext(_options);
        var log = await context.CodeSendLogs.SingleAsync(row => row.Outcome == "blocked-prefix-limit");
        Assert.Equal("sms", log.Channel);
        Assert.DoesNotContain(SamePrefixPhone, log.MaskedDestination);
        Assert.Contains('*', log.MaskedDestination);
        Assert.Equal(1, await context.ElectionSendControls.Select(row => row.PaidSendsUsed).SingleAsync());
        _alerts.Verify(alert => alert.NotifyPrefixLimitAsync(
            It.Is<AbusePrefixLimitAlert>(item =>
                item.Prefix == "141689"
                && item.Channel == "sms"
                && item.MaskedDestination.Contains('*')
                && !item.MaskedDestination.Contains(SamePrefixPhone)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ParallelConsumes_DoNotExceedTheCap()
    {
        _antiAbuse.PhonePrefixSendLimit = 5;
        var tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            await using var context = new MainDbContext(_options);
            var limiter = CreateLimiter(context);
            return await limiter.TryConsumeAsync("sms", TorontoPhone);
        }));

        var results = await Task.WhenAll(tasks);

        Assert.Equal(5, results.Count(result => result.Allowed));
        await using var check = new MainDbContext(_options);
        var row = await check.PhonePrefixSendCounters.SingleAsync();
        Assert.Equal(5, row.SendCount);
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

    private Task<PhonePrefixSendResult> ConsumeAsync(string destination, string channel = "sms")
    {
        return ConsumeOnNewContextAsync(destination, channel);
    }

    private async Task<PhonePrefixSendResult> ConsumeOnNewContextAsync(string destination, string channel)
    {
        await using var context = new MainDbContext(_options);
        return await CreateLimiter(context).TryConsumeAsync(channel, destination);
    }

    private PhonePrefixSendLimiter CreateLimiter(MainDbContext context)
    {
        return new PhonePrefixSendLimiter(
            context,
            Options.Create(_antiAbuse),
            _clock,
            NullLogger<PhonePrefixSendLimiter>.Instance);
    }

    private async Task<CodeSendReservation> ReserveAsync(string channel, string destination)
    {
        await using var context = new MainDbContext(_options);
        var guard = new CodeSendGuard(
            context,
            new PaidSendCounters(context),
            _alerts.Object,
            _clock,
            Options.Create(_antiAbuse),
            NullLogger<CodeSendGuard>.Instance,
            CreateLimiter(context));
        return await guard.ReserveAsync(new[] { _electionId }, channel, destination);
    }

    private sealed class MutableClock : ICodeSendClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.Parse("2026-10-07T12:00:00Z");

        public DateOnly UtcDate => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }
}
