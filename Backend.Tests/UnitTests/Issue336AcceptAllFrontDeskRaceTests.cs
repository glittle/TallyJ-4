using Backend.Context;
using Backend.DTOs.FrontDesk;
using Backend.DTOs.OnlineVoting;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests;

/// <summary>
/// #336 leftover: Accept-all vs Front Desk check-in in the same moment.
/// SQLite (not in-memory) so ExecuteUpdate/ExecuteDelete and identity
/// resolution across two contexts can see each other's commits.
/// </summary>
public class Issue336AcceptAllFrontDeskRaceTests
{
    [Fact]
    public async Task AcceptAllListedSubmitted_ThenDeskCheckIn_BeforePass2_DeskMethodOnly()
    {
        await using var db = await SqliteRaceDb.CreateAsync();
        var seed = await SeedSubmittedAsync(db);

        await using var acceptContext = db.CreateContext();
        var alreadyListed = await acceptContext.OnlineVotingInfos
            .Where(o => o.ElectionGuid == seed.ElectionGuid
                        && o.Status == OnlineBallotStatus.Submitted)
            .ToListAsync();
        Assert.Single(alreadyListed);

        await using var deskContext = db.CreateContext();
        await CheckInAsync(deskContext, seed, VotingMethodCodes.InPerson);

        var accept = await CreateOnlineService(acceptContext)
            .AcceptAllPendingAsync(seed.ElectionGuid);
        Assert.True(accept.Success);
        Assert.Equal(0, accept.AcceptedCount);

        await AssertAtMostOneCountedVoteAsync(db, seed, expectDeskMethod: VotingMethodCodes.InPerson);
    }

    [Fact]
    public async Task DeskCheckInCommitsFirst_ThenAcceptAll_NoOnlineBallot()
    {
        await using var db = await SqliteRaceDb.CreateAsync();
        var seed = await SeedSubmittedAsync(db);

        await using var deskContext = db.CreateContext();
        await CheckInAsync(deskContext, seed, VotingMethodCodes.Mailed);

        await using var acceptContext = db.CreateContext();
        var accept = await CreateOnlineService(acceptContext)
            .AcceptAllPendingAsync(seed.ElectionGuid);
        Assert.True(accept.Success);
        Assert.Equal(0, accept.AcceptedCount);

        await AssertAtMostOneCountedVoteAsync(db, seed, expectDeskMethod: VotingMethodCodes.Mailed);
    }

    [Fact]
    public async Task AcceptAllClaimedProcessing_DeskCheckInRefuses_AcceptAllMayFinishOneBallot()
    {
        await using var db = await SqliteRaceDb.CreateAsync();
        var seed = await SeedSubmittedAsync(db);

        await using var claimContext = db.CreateContext();
        var claimed = await claimContext.OnlineVotingInfos
            .Where(o => o.ElectionGuid == seed.ElectionGuid
                        && o.Status == OnlineBallotStatus.Submitted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, OnlineBallotStatus.Processing));
        Assert.Equal(1, claimed);

        await using var deskContext = db.CreateContext();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CheckInAsync(deskContext, seed, VotingMethodCodes.InPerson));
        Assert.Equal(FrontDeskMessageKeys.AlreadyProcessingOnline, ex.Message);

        await using var acceptContext = db.CreateContext();
        var accept = await CreateOnlineService(acceptContext)
            .AcceptAllPendingAsync(seed.ElectionGuid);
        Assert.True(accept.Success);
        Assert.Equal(1, accept.AcceptedCount);

        await AssertAtMostOneCountedVoteAsync(db, seed, expectDeskMethod: null);
    }

    [Fact]
    public async Task StaleDeskContext_AfterAcceptAllProcessed_DoesNotRecordSecondMethod()
    {
        await using var db = await SqliteRaceDb.CreateAsync();
        var seed = await SeedSubmittedAsync(db);

        await using var staleDesk = db.CreateContext();
        var tracked = await staleDesk.OnlineVotingInfos.SingleAsync();
        Assert.Equal(OnlineBallotStatus.Submitted, tracked.Status);

        await using var acceptContext = db.CreateContext();
        var accept = await CreateOnlineService(acceptContext)
            .AcceptAllPendingAsync(seed.ElectionGuid);
        Assert.True(accept.Success);
        Assert.Equal(1, accept.AcceptedCount);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CheckInAsync(staleDesk, seed, VotingMethodCodes.InPerson));
        Assert.Equal(FrontDeskMessageKeys.AlreadyAcceptedOnline, ex.Message);

        await AssertAtMostOneCountedVoteAsync(db, seed, expectDeskMethod: null);
    }

    [Fact]
    public async Task StaleDeskContext_AfterAcceptAllClaimedProcessing_DoesNotRecordSecondMethod()
    {
        await using var db = await SqliteRaceDb.CreateAsync();
        var seed = await SeedSubmittedAsync(db);

        await using var staleDesk = db.CreateContext();
        var tracked = await staleDesk.OnlineVotingInfos.SingleAsync();
        Assert.Equal(OnlineBallotStatus.Submitted, tracked.Status);

        await using var claimContext = db.CreateContext();
        var claimed = await claimContext.OnlineVotingInfos
            .Where(o => o.ElectionGuid == seed.ElectionGuid
                        && o.Status == OnlineBallotStatus.Submitted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, OnlineBallotStatus.Processing));
        Assert.Equal(1, claimed);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CheckInAsync(staleDesk, seed, VotingMethodCodes.DroppedOff));
        Assert.Equal(FrontDeskMessageKeys.AlreadyProcessingOnline, ex.Message);

        await using var acceptContext = db.CreateContext();
        var accept = await CreateOnlineService(acceptContext)
            .AcceptAllPendingAsync(seed.ElectionGuid);
        Assert.True(accept.Success);
        Assert.Equal(1, accept.AcceptedCount);

        await AssertAtMostOneCountedVoteAsync(db, seed, expectDeskMethod: null);
    }

    [Fact]
    public async Task AfterWithdrawAndUnregister_AcceptAllHasNothingToProcess()
    {
        await using var db = await SqliteRaceDb.CreateAsync();
        var seed = await SeedSubmittedAsync(db);

        await using var deskContext = db.CreateContext();
        await CheckInAsync(deskContext, seed, VotingMethodCodes.InPerson);
        await UnregisterAsync(deskContext, seed);

        await using var acceptContext = db.CreateContext();
        var accept = await CreateOnlineService(acceptContext)
            .AcceptAllPendingAsync(seed.ElectionGuid);
        Assert.True(accept.Success);
        Assert.Equal(0, accept.AcceptedCount);

        await using var check = db.CreateContext();
        Assert.Equal(0, await check.Ballots.CountAsync());
        Assert.Empty(await check.OnlineVotingInfos.ToListAsync());
        var person = await check.People.SingleAsync(p => p.PersonGuid == seed.PersonGuid);
        Assert.Null(person.VotingMethod);
        Assert.Null(person.RegistrationTime);
        Assert.False(person.HasOnlineBallot);
    }

    [Fact]
    public async Task Pass2AfterStaleExpectedId_WithdrawnRowCreatesNoBallot()
    {
        await using var db = await SqliteRaceDb.CreateAsync();
        var seed = await SeedSubmittedAsync(db);

        await using var listed = db.CreateContext();
        var rowId = await listed.OnlineVotingInfos
            .Where(o => o.ElectionGuid == seed.ElectionGuid)
            .Select(o => o.RowId)
            .SingleAsync();

        await using var deskContext = db.CreateContext();
        await CheckInAsync(deskContext, seed, VotingMethodCodes.InPerson);

        await using var leftoverClaim = db.CreateContext();
        var claimed = await leftoverClaim.OnlineVotingInfos
            .Where(o => o.ElectionGuid == seed.ElectionGuid
                        && o.Status == OnlineBallotStatus.Submitted
                        && o.RowId == rowId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, OnlineBallotStatus.Processing));
        Assert.Equal(0, claimed);

        await using var leftoverTake = db.CreateContext();
        var taken = await leftoverTake.OnlineVotingInfos
            .Where(o => o.RowId == rowId
                        && o.ElectionGuid == seed.ElectionGuid
                        && o.Status == OnlineBallotStatus.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, OnlineBallotStatus.Processed));
        Assert.Equal(0, taken);

        await AssertAtMostOneCountedVoteAsync(db, seed, expectDeskMethod: VotingMethodCodes.InPerson);
    }

    private static async Task AssertAtMostOneCountedVoteAsync(
        SqliteRaceDb db,
        SeededElection seed,
        string? expectDeskMethod)
    {
        await using var check = db.CreateContext();
        var person = await check.People.SingleAsync(p => p.PersonGuid == seed.PersonGuid);
        var online = await check.OnlineVotingInfos
            .SingleOrDefaultAsync(o => o.PersonGuid == seed.PersonGuid);
        var onlineBallots = await check.Ballots.CountAsync(b =>
            b.ComputerCode == ComputerCodeHelper.Online);

        var hasDeskMethod = VotingMethodCodes.IsRecordedOtherThanOnline(person.VotingMethod);
        var hasProcessedOnline = online != null && OnlineBallotStatus.IsProcessed(online.Status);
        var countedPaths = (hasDeskMethod ? 1 : 0)
            + (hasProcessedOnline || onlineBallots > 0 ? 1 : 0);

        Assert.True(countedPaths <= 1, "Desk method plus an Online ballot is two counted votes.");
        Assert.True(onlineBallots <= 1);
        Assert.Equal(expectDeskMethod, person.VotingMethod);

        if (expectDeskMethod != null)
        {
            Assert.Equal(0, onlineBallots);
            Assert.True(online == null || !OnlineBallotStatus.IsProcessed(online.Status));
            Assert.Empty(await check.OnlineVotingInfos.ToListAsync());
        }
        else
        {
            Assert.Null(person.RegistrationTime);
            Assert.Equal(1, onlineBallots);
            Assert.NotNull(online);
            Assert.Equal(OnlineBallotStatus.Processed, online.Status);
        }
    }

    private static Task<FrontDeskVoterDto> CheckInAsync(
        MainDbContext context,
        SeededElection seed,
        string method) =>
        CreateFrontDesk(context).CheckInVoterAsync(seed.ElectionGuid, new CheckInVoterDto
        {
            PersonGuid = seed.PersonGuid,
            VotingMethod = method,
            Teller1 = "Ada"
        });

    private static Task<FrontDeskVoterDto> UnregisterAsync(
        MainDbContext context,
        SeededElection seed) =>
        CreateFrontDesk(context).UnregisterVoterAsync(seed.ElectionGuid, new UnregisterVoterDto
        {
            PersonGuid = seed.PersonGuid,
            Reason = "Change method"
        });

    private static FrontDeskService CreateFrontDesk(MainDbContext context) =>
        new(
            context,
            Mock.Of<ILogger<FrontDeskService>>(),
            Mock.Of<ISignalRNotificationService>());

    private static OnlineVotingService CreateOnlineService(MainDbContext context)
    {
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.Setup(e => e.EnvironmentName).Returns("Testing");
        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<MimeKit.MimeMessage>()))
            .Returns(Task.CompletedTask);

        return new OnlineVotingService(
            context,
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            hostEnvironment.Object,
            Mock.Of<ILogger<OnlineVotingService>>(),
            Mock.Of<IHttpClientFactory>(),
            emailSender.Object,
            Mock.Of<IPaidVerificationSender>(),
            Mock.Of<IGoogleIdTokenValidator>(),
            Mock.Of<ISignalRNotificationService>(),
            new AlwaysAllowAcceptLock());
    }

    private static async Task<SeededElection> SeedSubmittedAsync(SqliteRaceDb db)
    {
        await using var context = db.CreateContext();
        var electionGuid = Guid.NewGuid();
        var personGuid = Guid.NewGuid();
        var email = $"race_{Guid.NewGuid():N}@example.com";
        context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Accept-all vs Front Desk race",
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTimeOffset.UtcNow.AddHours(1),
            ElectionStage = ElectionStage.GatheringBallots,
            NumberToElect = 9,
            RowVersion = new byte[8]
        });
        context.People.Add(new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = personGuid,
            FirstName = "Ada",
            LastName = "Smith",
            Email = email,
            CanVote = true,
            CanReceiveVotes = true,
            RowVersion = new byte[8]
        });
        context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = email,
            VoterIdType = "E",
            WhenRegistered = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        var submit = await CreateOnlineService(context).SubmitBallotAsync(new SubmitOnlineBallotDto
        {
            ElectionGuid = electionGuid,
            VoterId = email,
            Votes =
            [
                new OnlineVoteDto
                {
                    PersonGuid = personGuid,
                    VoteName = "Ada Smith",
                    PositionOnBallot = 1
                }
            ]
        });
        Assert.True(submit.Success);
        return new SeededElection(electionGuid, personGuid, email);
    }

    private sealed record SeededElection(Guid ElectionGuid, Guid PersonGuid, string Email);

    private sealed class AlwaysAllowAcceptLock : IOnlineBallotAcceptLock
    {
        public bool TryEnter(Guid electionGuid) => true;

        public void Exit(Guid electionGuid)
        {
        }
    }

    private sealed class SqliteRaceDb : IAsyncDisposable
    {
        private SqliteRaceDb(string dbPath, DbContextOptions<MainDbContext> options)
        {
            DbPath = dbPath;
            Options = options;
        }

        public string DbPath { get; }

        public DbContextOptions<MainDbContext> Options { get; }

        public static async Task<SqliteRaceDb> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"tallyj-fd-race-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<MainDbContext>()
                .UseSqlite($"Data Source={path}", b => b.CommandTimeout(30))
                .Options;
            await using var context = new MainDbContext(options);
            await context.Database.EnsureCreatedAsync();
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            await context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");
            return new SqliteRaceDb(path, options);
        }

        public MainDbContext CreateContext()
        {
            var context = new MainDbContext(Options);
            context.Database.OpenConnection();
            context.Database.ExecuteSqlRaw("PRAGMA busy_timeout=5000;");
            return context;
        }

        public async ValueTask DisposeAsync()
        {
            SqliteConnection.ClearAllPools();
            try
            {
                if (File.Exists(DbPath))
                {
                    File.Delete(DbPath);
                }
            }
            catch (IOException)
            {
            }

            await Task.CompletedTask;
        }
    }
}
