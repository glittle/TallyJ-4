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
/// Paid-send approval, caps, freeze, and flag. Counters use SQLite UPDATE ... RETURNING,
/// the same statement shape the SQL Server OUTPUT path uses.
/// </summary>
public class CodeSendGuardTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<MainDbContext> _options;
    private readonly MutableClock _clock = new();
    private readonly Mock<IAbuseAlertService> _alerts = new();
    private readonly Guid _electionId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly AntiAbuseOptions _antiAbuse = new()
    {
        ElectionPaidSendAllowance = 25,
        OwnerDailyPaidSendCap = 100
    };

    public CodeSendGuardTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"paid-send-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<MainDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        using var context = new MainDbContext(_options);
        context.Database.EnsureCreated();
        context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        context.Elections.Add(new Election
        {
            ElectionGuid = _electionId,
            Name = "Paid send election",
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-1),
            ElectionStage = ElectionStage.GatheringBallots,
            RowVersion = new byte[8]
        });
        context.People.Add(new Person
        {
            ElectionGuid = _electionId,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Voter",
            Phone = "+14165550100",
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
        context.SaveChanges();
    }

    [Fact]
    public async Task Email_IsSent_WhenOwnerIsNotApproved()
    {
        var reservation = await ReserveAsync("email", "ada@example.com");

        Assert.True(reservation.Allowed);
        Assert.Null(reservation.BlockReason);
        Assert.Equal(0, await PaidSendsUsedAsync());
    }

    [Fact]
    public async Task Sms_IsBlocked_WhenOwnerIsNotApproved()
    {
        var reservation = await ReserveAsync("sms", "+14165550100");

        Assert.False(reservation.Allowed);
        Assert.Equal(CodeSendBlockReason.NotApproved, reservation.BlockReason);
        Assert.Equal(0, await PaidSendsUsedAsync());
        Assert.Equal(
            "blocked-not-approved",
            await LogOutcomeAsync());
    }

    [Fact]
    public async Task Sms_IsSent_WhenOwnerIsApproved()
    {
        await ApproveOwnerAsync();

        var reservation = await ReserveAsync("sms", "+14165550100");

        Assert.True(reservation.Allowed);
        Assert.True(reservation.FirstPaidSend);
        Assert.Equal(1, await PaidSendsUsedAsync());
        Assert.Equal(1, await OwnerDayCountAsync());
        _alerts.Verify(alert => alert.NotifyFirstPaidSendAsync(
            It.Is<AbuseFirstPaidSendAlert>(item => item.OwnerUserId == _ownerId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VoiceAndWhatsApp_UseTheSameApprovalGate()
    {
        await ReserveAsync("voice", "+14165550100");
        await ReserveAsync("whatsapp", "+14165550100");

        Assert.Equal(0, await PaidSendsUsedAsync());
    }

    [Fact]
    public async Task ElectionAllowance_StopsSending_UntilRaised()
    {
        _antiAbuse.ElectionPaidSendAllowance = 2;
        await ApproveOwnerAsync();

        Assert.True((await ReserveAsync("sms", "+14165550100")).Allowed);
        Assert.True((await ReserveAsync("sms", "+14165550101")).Allowed);
        var blocked = await ReserveAsync("sms", "+14165550102");

        Assert.False(blocked.Allowed);
        Assert.Equal(CodeSendBlockReason.ElectionCap, blocked.BlockReason);
        Assert.Equal(2, await PaidSendsUsedAsync());
        _alerts.Verify(alert => alert.NotifyCapHitAsync(
            It.Is<AbuseCapHitAlert>(item => item.Scope == "election" && item.Cap == 2),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        await using var context = new MainDbContext(_options);
        var control = await context.ElectionSendControls.SingleAsync();
        control.AllowanceOverride = 3;
        await context.SaveChangesAsync();

        Assert.True((await ReserveAsync("sms", "+14165550102")).Allowed);
        Assert.Equal(3, await PaidSendsUsedAsync());
    }

    [Fact]
    public async Task OwnerDailyCap_StopsForTheUtcDay_AndResetsTheNextDay()
    {
        _antiAbuse.OwnerDailyPaidSendCap = 1;
        await ApproveOwnerAsync();

        Assert.True((await ReserveAsync("sms", "+14165550100")).Allowed);
        var blocked = await ReserveAsync("whatsapp", "+14165550100");
        Assert.False(blocked.Allowed);
        Assert.Equal(CodeSendBlockReason.OwnerDailyCap, blocked.BlockReason);

        _clock.UtcNow = _clock.UtcNow.AddDays(1);
        Assert.True((await ReserveAsync("sms", "+14165550100")).Allowed);
        Assert.Equal(2, await PaidSendsUsedAsync());
    }

    [Fact]
    public async Task ConcurrentReserves_DoNotExceedTheElectionAllowance()
    {
        _antiAbuse.ElectionPaidSendAllowance = 3;
        await ApproveOwnerAsync();

        var tasks = Enumerable.Range(0, 8)
            .Select(_ => ReserveAsync("sms", "+14165550100"))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        Assert.Equal(3, results.Count(result => result.Allowed));
        Assert.Equal(3, await PaidSendsUsedAsync());
    }

    [Fact]
    public async Task FreezeElection_BlocksEmailAndSms()
    {
        await ApproveOwnerAsync();
        await using (var context = new MainDbContext(_options))
        {
            context.ElectionSendControls.Add(new ElectionSendControl
            {
                ElectionGuid = _electionId,
                SendsFrozen = true
            });
            await context.SaveChangesAsync();
        }

        Assert.False((await ReserveAsync("email", "ada@example.com")).Allowed);
        Assert.Equal(CodeSendBlockReason.Frozen, (await ReserveAsync("sms", "+14165550100")).BlockReason);
        Assert.Equal(0, await PaidSendsUsedAsync());
    }

    [Fact]
    public async Task FrozenCoOwner_BlocksEmailAndSms_WhenTheOtherAdminIsApproved()
    {
        var coOwner = Guid.NewGuid();
        await using (var context = new MainDbContext(_options))
        {
            context.JoinElectionUsers.Add(new JoinElectionUser
            {
                ElectionGuid = _electionId,
                UserId = coOwner,
                Role = "Owner"
            });
            context.OwnerSendControls.AddRange(
                new OwnerSendControl
                {
                    UserId = _ownerId,
                    PaidSendsApproved = true,
                    ApprovedAt = _clock.UtcNow
                },
                new OwnerSendControl
                {
                    UserId = coOwner,
                    PaidSendsApproved = true,
                    ApprovedAt = _clock.UtcNow,
                    SendsFrozen = true
                });
            await context.SaveChangesAsync();
        }

        var email = await ReserveAsync("email", "ada@example.com");
        Assert.False(email.Allowed);
        Assert.Equal(CodeSendBlockReason.Frozen, email.BlockReason);
        Assert.Equal(CodeSendBlockReason.Frozen, (await ReserveAsync("sms", "+14165550100")).BlockReason);
        Assert.Equal(0, await PaidSendsUsedAsync());
    }

    [Fact]
    public async Task UnapprovedCoOwner_BlocksSms_AndStillAllowsEmail()
    {
        var coOwner = Guid.NewGuid();
        await using (var context = new MainDbContext(_options))
        {
            context.JoinElectionUsers.Add(new JoinElectionUser
            {
                ElectionGuid = _electionId,
                UserId = coOwner,
                Role = "Admin"
            });
            context.OwnerSendControls.Add(new OwnerSendControl
            {
                UserId = _ownerId,
                PaidSendsApproved = true,
                ApprovedAt = _clock.UtcNow
            });
            await context.SaveChangesAsync();
        }

        Assert.True((await ReserveAsync("email", "ada@example.com")).Allowed);
        var sms = await ReserveAsync("sms", "+14165550100");
        Assert.False(sms.Allowed);
        Assert.Equal(CodeSendBlockReason.NotApproved, sms.BlockReason);
        Assert.Equal(0, await PaidSendsUsedAsync());
    }

    [Fact]
    public async Task Sms_IsSent_WhenEveryCoOwnerIsApproved()
    {
        var coOwner = Guid.NewGuid();
        await using (var context = new MainDbContext(_options))
        {
            context.JoinElectionUsers.Add(new JoinElectionUser
            {
                ElectionGuid = _electionId,
                UserId = coOwner,
                Role = "Owner"
            });
            context.OwnerSendControls.AddRange(
                new OwnerSendControl
                {
                    UserId = _ownerId,
                    PaidSendsApproved = true,
                    ApprovedAt = _clock.UtcNow
                },
                new OwnerSendControl
                {
                    UserId = coOwner,
                    PaidSendsApproved = true,
                    ApprovedAt = _clock.UtcNow
                });
            await context.SaveChangesAsync();
        }

        Assert.True((await ReserveAsync("sms", "+14165550100")).Allowed);
        Assert.Equal(1, await PaidSendsUsedAsync());
    }

    [Fact]
    public async Task FreezeOwner_BlocksEmailWhenThatOwnerIsTheOnlyAdmin()
    {
        await using (var context = new MainDbContext(_options))
        {
            context.OwnerSendControls.Add(new OwnerSendControl
            {
                UserId = _ownerId,
                PaidSendsApproved = true,
                SendsFrozen = true
            });
            await context.SaveChangesAsync();
        }

        var email = await ReserveAsync("email", "ada@example.com");
        Assert.False(email.Allowed);
        Assert.Equal(CodeSendBlockReason.Frozen, email.BlockReason);
    }

    [Fact]
    public async Task FlaggedElection_BlocksEveryChannel()
    {
        await ApproveOwnerAsync();
        await using (var context = new MainDbContext(_options))
        {
            context.ElectionSendControls.Add(new ElectionSendControl
            {
                ElectionGuid = _electionId,
                Flagged = true,
                FlaggedAt = _clock.UtcNow,
                FlaggedEntryCount = 4
            });
            await context.SaveChangesAsync();
        }

        Assert.Equal(CodeSendBlockReason.Flagged, (await ReserveAsync("email", "ada@example.com")).BlockReason);
        Assert.Equal(CodeSendBlockReason.Flagged, (await ReserveAsync("sms", "+14165550100")).BlockReason);
    }

    [Fact]
    public async Task FirstPaidSend_AlertsOnce()
    {
        await ApproveOwnerAsync();
        Assert.True((await ReserveAsync("sms", "+14165550100")).FirstPaidSend);
        Assert.False((await ReserveAsync("sms", "+14165550101")).FirstPaidSend);
        _alerts.Verify(alert => alert.NotifyFirstPaidSendAsync(
            It.IsAny<AbuseFirstPaidSendAlert>(),
            It.IsAny<CancellationToken>()), Times.Once);
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

    private async Task<CodeSendReservation> ReserveAsync(string channel, string destination)
    {
        await using var context = new MainDbContext(_options);
        var guard = new CodeSendGuard(
            context,
            new PaidSendCounters(context),
            _alerts.Object,
            _clock,
            Options.Create(_antiAbuse),
            NullLogger<CodeSendGuard>.Instance);
        return await guard.ReserveAsync(new[] { _electionId }, channel, destination);
    }

    private async Task ApproveOwnerAsync()
    {
        await using var context = new MainDbContext(_options);
        context.OwnerSendControls.Add(new OwnerSendControl
        {
            UserId = _ownerId,
            PaidSendsApproved = true,
            ApprovedAt = _clock.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private async Task<int> PaidSendsUsedAsync()
    {
        await using var context = new MainDbContext(_options);
        return await context.ElectionSendControls
            .Where(row => row.ElectionGuid == _electionId)
            .Select(row => row.PaidSendsUsed)
            .SingleOrDefaultAsync();
    }

    private async Task<int> OwnerDayCountAsync()
    {
        await using var context = new MainDbContext(_options);
        return await context.OwnerDailyPaidSends
            .Where(row => row.UserId == _ownerId && row.UtcDate == _clock.UtcDate)
            .Select(row => row.SendCount)
            .SingleOrDefaultAsync();
    }

    private async Task<string?> LogOutcomeAsync()
    {
        await using var context = new MainDbContext(_options);
        return await context.CodeSendLogs
            .OrderByDescending(row => row.RowId)
            .Select(row => row.Outcome)
            .FirstOrDefaultAsync();
    }

    private sealed class MutableClock : ICodeSendClock
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

        public DateOnly UtcDate => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }
}
