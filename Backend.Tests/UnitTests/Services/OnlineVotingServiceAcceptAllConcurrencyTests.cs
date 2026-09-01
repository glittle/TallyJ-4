using Backend.Context;
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

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// SQLite tests for the DB compare-and-swap. In-memory EF has no row locks and
/// ignores transactions, so it cannot prove the claim. These would have failed
/// on the pre-claim Accept-all (status re-read + UPDATE by PK).
/// </summary>
public class OnlineVotingServiceAcceptAllConcurrencyTests
{
    [Fact]
    public async Task TwoAcceptAlls_WithoutInProcessLock_CreateOnlyOneBallot()
    {
        await using var db = await SqliteOnlineVotingDb.CreateAsync();
        var election = await SeedPendingVoterAsync(db);

        await using var context1 = db.CreateContext();
        await using var context2 = db.CreateContext();
        var service1 = CreateService(context1, new AlwaysAllowAcceptLock());
        var service2 = CreateService(context2, new AlwaysAllowAcceptLock());

        var results = await Task.WhenAll(
            service1.AcceptAllPendingAsync(election.ElectionGuid),
            service2.AcceptAllPendingAsync(election.ElectionGuid));

        Assert.All(results, r => Assert.True(r.Success));
        Assert.Equal(1, results.Sum(r => r.AcceptedCount));

        await using var check = db.CreateContext();
        Assert.Equal(1, await check.Ballots.CountAsync());
        Assert.Equal(
            OnlineBallotStatus.Processed,
            (await check.OnlineVotingInfos.SingleAsync()).Status);
    }

    [Fact]
    public async Task SubmitWrite_OnStaleSubmittedEntity_DoesNotReviveAfterAccept()
    {
        await using var db = await SqliteOnlineVotingDb.CreateAsync();
        var election = await SeedPendingVoterAsync(db);

        await using var staleContext = db.CreateContext();
        var staleRow = await staleContext.OnlineVotingInfos.SingleAsync();
        Assert.Equal(OnlineBallotStatus.Submitted, staleRow.Status);

        await using var acceptContext = db.CreateContext();
        var accept = await CreateService(acceptContext, new AlwaysAllowAcceptLock())
            .AcceptAllPendingAsync(election.ElectionGuid);
        Assert.True(accept.Success);
        Assert.Equal(1, accept.AcceptedCount);

        var wrote = await CreateService(staleContext, new AlwaysAllowAcceptLock())
            .TryWritePendingPayloadIfStillSubmittedAsync(
                staleRow,
                """{"votes":[],"pool":[]}""",
                DateTimeOffset.UtcNow);
        Assert.False(wrote);

        await using var check = db.CreateContext();
        Assert.Equal(1, await check.Ballots.CountAsync());
        var ovi = await check.OnlineVotingInfos.SingleAsync();
        Assert.Equal(OnlineBallotStatus.Processed, ovi.Status);
        Assert.Null(ovi.ListPool);
        Assert.Null(ovi.BallotGuid);
    }

    [Fact]
    public async Task SecondServer_CompletesLeftoverProcessing_WithoutSecondBallot()
    {
        await using var db = await SqliteOnlineVotingDb.CreateAsync();
        var election = await SeedPendingVoterAsync(db);

        await using var claimContext = db.CreateContext();
        var claimed = await claimContext.OnlineVotingInfos
            .Where(o => o.ElectionGuid == election.ElectionGuid
                        && o.Status == OnlineBallotStatus.Submitted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, OnlineBallotStatus.Processing));
        Assert.Equal(1, claimed);

        await using var acceptContext = db.CreateContext();
        var result = await CreateService(acceptContext, new AlwaysAllowAcceptLock())
            .AcceptAllPendingAsync(election.ElectionGuid);
        Assert.True(result.Success);
        Assert.Equal(1, result.AcceptedCount);

        await using var check = db.CreateContext();
        Assert.Equal(1, await check.Ballots.CountAsync());
        Assert.Equal(
            OnlineBallotStatus.Processed,
            (await check.OnlineVotingInfos.SingleAsync()).Status);
    }

    [Fact]
    public async Task SequentialAcceptAlls_WithoutInProcessLock_SecondCreatesNoBallot()
    {
        await using var db = await SqliteOnlineVotingDb.CreateAsync();
        var election = await SeedPendingVoterAsync(db);

        await using var context1 = db.CreateContext();
        var first = await CreateService(context1, new AlwaysAllowAcceptLock())
            .AcceptAllPendingAsync(election.ElectionGuid);
        Assert.Equal(1, first.AcceptedCount);

        await using var context2 = db.CreateContext();
        var second = await CreateService(context2, new AlwaysAllowAcceptLock())
            .AcceptAllPendingAsync(election.ElectionGuid);
        Assert.True(second.Success);
        Assert.Equal(0, second.AcceptedCount);

        await using var check = db.CreateContext();
        Assert.Equal(1, await check.Ballots.CountAsync());
    }

    private static OnlineVotingService CreateService(
        MainDbContext context,
        IOnlineBallotAcceptLock acceptLock)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.Setup(e => e.EnvironmentName).Returns("Testing");
        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<MimeKit.MimeMessage>()))
            .Returns(Task.CompletedTask);

        return new OnlineVotingService(
            context,
            configuration,
            hostEnvironment.Object,
            Mock.Of<ILogger<OnlineVotingService>>(),
            Mock.Of<IHttpClientFactory>(),
            emailSender.Object,
            Mock.Of<IPaidVerificationSender>(),
            Mock.Of<IGoogleIdTokenValidator>(),
            Mock.Of<ISignalRNotificationService>(),
            acceptLock);
    }

    private static async Task<SeededElection> SeedPendingVoterAsync(SqliteOnlineVotingDb db)
    {
        await using var context = db.CreateContext();
        var electionGuid = Guid.NewGuid();
        var personGuid = Guid.NewGuid();
        var email = $"voter_{Guid.NewGuid():N}@example.com";
        context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Accept-all concurrency",
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
            FirstName = "Test",
            LastName = "Voter",
            Email = email,
            CanVote = true,
            RowVersion = new byte[8]
        });
        context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = email,
            VoterIdType = "E",
            WhenRegistered = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context, new AlwaysAllowAcceptLock());
        var submit = await service.SubmitBallotAsync(new SubmitOnlineBallotDto
        {
            ElectionGuid = electionGuid,
            VoterId = email,
            Votes =
            [
                new OnlineVoteDto
                {
                    PersonGuid = personGuid,
                    VoteName = "Free Voter",
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

    private sealed class SqliteOnlineVotingDb : IAsyncDisposable
    {
        private SqliteOnlineVotingDb(string dbPath, DbContextOptions<MainDbContext> options)
        {
            DbPath = dbPath;
            Options = options;
        }

        public string DbPath { get; }

        public DbContextOptions<MainDbContext> Options { get; }

        public static async Task<SqliteOnlineVotingDb> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"tallyj-accept-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<MainDbContext>()
                .UseSqlite($"Data Source={path}", b => b.CommandTimeout(30))
                .Options;
            await using var context = new MainDbContext(options);
            await context.Database.EnsureCreatedAsync();
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            await context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");
            return new SqliteOnlineVotingDb(path, options);
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
