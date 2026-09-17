using Backend.Context;
using Backend.DTOs.Ballots;
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
/// #191 leftover: two tellers working at the same moment leave paper
/// ballots, Accept-all, and Front Desk with one counted path per person.
/// SQLite (not in-memory) so two DbContexts can see each other's commits.
/// Ordered Accept-all vs check-in interleavings stay in
/// <see cref="Issue336AcceptAllFrontDeskRaceTests"/>; two Accept-alls stay
/// in <c>OnlineVotingServiceAcceptAllConcurrencyTests</c>.
/// </summary>
public class Issue191ConcurrentTellerTests
{
    [Fact]
    public async Task TwoTellers_EnterPaperBallotsOnDifferentComputers_BothPersistWithPerComputerNumbers()
    {
        await using var db = await SqliteConcurrentDb.CreateAsync();
        var seed = await SeedElectionAsync(db, submittedVoters: 0);

        await using var tellerA = db.CreateContext();
        await using var tellerB = db.CreateContext();

        var created = await Task.WhenAll(
            CreatePaperBallotAsync(tellerA, seed, "A", "Ada"),
            CreatePaperBallotAsync(tellerB, seed, "B", "Ben"));

        Assert.Equal(2, created.Select(b => b.BallotGuid).Distinct().Count());
        Assert.Equal("A1", created.Single(b => b.ComputerCode == "A").BallotCode);
        Assert.Equal("B1", created.Single(b => b.ComputerCode == "B").BallotCode);

        await using var check = db.CreateContext();
        var ballots = await check.Ballots
            .Where(b => b.LocationGuid == seed.PaperLocationGuid)
            .OrderBy(b => b.ComputerCode)
            .ThenBy(b => b.BallotNumAtComputer)
            .ToListAsync();
        Assert.Equal(2, ballots.Count);
        Assert.Equal(2, ballots.Select(b => b.BallotGuid).Distinct().Count());
        Assert.Equal(["A1", "B1"], ballots.Select(b => b.BallotCode));
        Assert.Equal(1, ballots.Single(b => b.ComputerCode == "A").BallotNumAtComputer);
        Assert.Equal(1, ballots.Single(b => b.ComputerCode == "B").BallotNumAtComputer);
        Assert.Equal(0, await check.Ballots.CountAsync(b =>
            b.ComputerCode == ComputerCodeHelper.Online));
    }

    [Fact]
    public async Task TwoTellers_EachEnterTwoPaperBallotsConcurrently_SequencesStayPerComputer()
    {
        await using var db = await SqliteConcurrentDb.CreateAsync();
        var seed = await SeedElectionAsync(db, submittedVoters: 0);

        await using var tellerA = db.CreateContext();
        await using var tellerB = db.CreateContext();

        await Task.WhenAll(
            CreatePaperBallotsAsync(tellerA, seed, "A", "Ada", count: 2),
            CreatePaperBallotsAsync(tellerB, seed, "B", "Ben", count: 2));

        await using var check = db.CreateContext();
        var ballots = await check.Ballots
            .Where(b => b.LocationGuid == seed.PaperLocationGuid)
            .ToListAsync();
        Assert.Equal(4, ballots.Count);
        Assert.Equal(4, ballots.Select(b => b.BallotGuid).Distinct().Count());
        Assert.Equal(["A1", "A2"], CodesFor(ballots, "A"));
        Assert.Equal(["B1", "B2"], CodesFor(ballots, "B"));
        Assert.Equal(4, ballots.Select(b => (b.ComputerCode, b.BallotNumAtComputer)).Distinct().Count());
    }

    [Fact]
    public async Task TellerA_AcceptAll_WhileTellerB_FrontDeskCheckIn_AtMostOneCountedVote()
    {
        await using var db = await SqliteConcurrentDb.CreateAsync();
        var seed = await SeedElectionAsync(db, submittedVoters: 1);
        var pending = seed.Submitted[0];

        await using var acceptContext = db.CreateContext();
        await using var deskContext = db.CreateContext();

        var acceptTask = CreateOnlineService(acceptContext)
            .AcceptAllPendingAsync(seed.ElectionGuid);
        var deskTask = CheckInAsync(deskContext, seed, pending, VotingMethodCodes.InPerson);

        Exception? deskError = null;
        try
        {
            await Task.WhenAll(acceptTask, deskTask);
        }
        catch (Exception ex)
        {
            deskError = Unwrap(ex);
        }

        var accept = await acceptTask;
        Assert.True(accept.Success);
        Assert.InRange(accept.AcceptedCount, 0, 1);

        if (deskError != null)
        {
            Assert.IsType<InvalidOperationException>(deskError);
            Assert.Contains(deskError.Message, new[]
            {
                FrontDeskMessageKeys.AlreadyAcceptedOnline,
                FrontDeskMessageKeys.AlreadyProcessingOnline
            });
        }

        await AssertAtMostOneCountedVoteAsync(db, seed, pending);
        await AssertElectionTotalsAsync(
            db,
            seed,
            expectedPaper: 0,
            expectedOnlineBallots: accept.AcceptedCount,
            expectedProcessedOnline: accept.AcceptedCount,
            expectedPendingOnline: 0);
    }

    [Fact]
    public async Task TellerA_PaperBallot_WhileTellerB_AcceptAll_OnDifferentPendingVoter_BothKept()
    {
        await using var db = await SqliteConcurrentDb.CreateAsync();
        var seed = await SeedElectionAsync(db, submittedVoters: 1);

        await using var paperContext = db.CreateContext();
        await using var acceptContext = db.CreateContext();

        var paperTask = CreatePaperBallotAsync(paperContext, seed, "A", "Ada");
        var acceptTask = AcceptAllAsync(acceptContext, seed.ElectionGuid);
        await Task.WhenAll(paperTask, acceptTask);

        var paper = await paperTask;
        var accept = await acceptTask;

        Assert.Equal("A1", paper.BallotCode);
        Assert.Equal(seed.PaperLocationGuid, paper.LocationGuid);
        Assert.True(accept.Success);
        Assert.Equal(1, accept.AcceptedCount);

        await using var check = db.CreateContext();
        var paperRows = await check.Ballots
            .Where(b => b.LocationGuid == seed.PaperLocationGuid)
            .ToListAsync();
        Assert.Single(paperRows);
        Assert.Equal("A", paperRows[0].ComputerCode);
        Assert.Equal(1, paperRows[0].BallotNumAtComputer);
        Assert.Equal(paper.BallotGuid, paperRows[0].BallotGuid);

        var onlineRows = await check.Ballots
            .Where(b => b.ComputerCode == ComputerCodeHelper.Online)
            .ToListAsync();
        Assert.Single(onlineRows);
        Assert.Equal($"{ComputerCodeHelper.Online}1", onlineRows[0].BallotCode);
        Assert.NotEqual(paper.BallotGuid, onlineRows[0].BallotGuid);

        var ovi = await check.OnlineVotingInfos.SingleAsync(o =>
            o.PersonGuid == seed.Submitted[0].PersonGuid);
        Assert.Equal(OnlineBallotStatus.Processed, ovi.Status);

        var paperPerson = await check.People.SingleAsync(p =>
            p.PersonGuid == seed.PaperVoter.PersonGuid);
        Assert.Null(paperPerson.VotingMethod);
        Assert.NotEqual(true, paperPerson.HasOnlineBallot);

        await AssertElectionTotalsAsync(
            db,
            seed,
            expectedPaper: 1,
            expectedOnlineBallots: 1,
            expectedProcessedOnline: 1,
            expectedPendingOnline: 0);
    }

    [Fact]
    public async Task TellerA_UnregisterAndRecheckIn_WhileTellerB_EntersPaperBallot_NoCrossTalk()
    {
        await using var db = await SqliteConcurrentDb.CreateAsync();
        var seed = await SeedElectionAsync(db, submittedVoters: 0);

        await using var deskSetup = db.CreateContext();
        await CheckInAsync(deskSetup, seed, seed.PaperVoter, VotingMethodCodes.InPerson);

        await using var deskContext = db.CreateContext();
        await using var paperContext = db.CreateContext();

        await Task.WhenAll(
            UnregisterAsync(deskContext, seed, seed.PaperVoter),
            CreatePaperBallotAsync(paperContext, seed, "B", "Ben"));

        await CheckInAsync(deskContext, seed, seed.PaperVoter, VotingMethodCodes.Mailed);

        await using var check = db.CreateContext();
        var person = await check.People.SingleAsync(p =>
            p.PersonGuid == seed.PaperVoter.PersonGuid);
        Assert.Equal(VotingMethodCodes.Mailed, person.VotingMethod);
        Assert.NotNull(person.RegistrationTime);
        Assert.NotEqual(true, person.HasOnlineBallot);

        var ballots = await check.Ballots.ToListAsync();
        Assert.Single(ballots);
        Assert.Equal("B", ballots[0].ComputerCode);
        Assert.Equal("B1", ballots[0].BallotCode);
        Assert.Equal(seed.PaperLocationGuid, ballots[0].LocationGuid);
        Assert.Empty(await check.OnlineVotingInfos.ToListAsync());
    }

    private static IReadOnlyList<string> CodesFor(IEnumerable<Ballot> ballots, string computerCode) =>
        ballots
            .Where(b => b.ComputerCode == computerCode)
            .OrderBy(b => b.BallotNumAtComputer)
            .Select(b => b.BallotCode ?? string.Empty)
            .ToList();

    private static async Task AssertAtMostOneCountedVoteAsync(
        SqliteConcurrentDb db,
        SeededElection seed,
        SeededPerson person)
    {
        await using var check = db.CreateContext();
        var row = await check.People.SingleAsync(p => p.PersonGuid == person.PersonGuid);
        var online = await check.OnlineVotingInfos
            .SingleOrDefaultAsync(o => o.PersonGuid == person.PersonGuid);
        var onlineBallots = await check.Ballots.CountAsync(b =>
            b.ComputerCode == ComputerCodeHelper.Online);

        var hasDeskMethod = VotingMethodCodes.IsRecordedOtherThanOnline(row.VotingMethod);
        var hasProcessedOnline = online != null && OnlineBallotStatus.IsProcessed(online.Status);
        var countedPaths = (hasDeskMethod ? 1 : 0)
            + (hasProcessedOnline || onlineBallots > 0 ? 1 : 0);

        Assert.True(countedPaths <= 1, "Desk method plus an Online ballot is two counted votes.");
        Assert.True(onlineBallots <= 1);
        Assert.True(
            VotingMethodCodes.HasVotedForCounts(row.VotingMethod, hasProcessedOnline),
            "The pending voter should still have exactly one counted path.");

        if (hasDeskMethod)
        {
            Assert.Equal(0, onlineBallots);
            Assert.True(online == null || !OnlineBallotStatus.IsProcessed(online.Status));
            Assert.Empty(await check.OnlineVotingInfos.ToListAsync());
        }
        else
        {
            Assert.Null(row.RegistrationTime);
            Assert.Equal(1, onlineBallots);
            Assert.NotNull(online);
            Assert.Equal(OnlineBallotStatus.Processed, online.Status);
        }
    }

    private static async Task AssertElectionTotalsAsync(
        SqliteConcurrentDb db,
        SeededElection seed,
        int expectedPaper,
        int expectedOnlineBallots,
        int expectedProcessedOnline,
        int expectedPendingOnline)
    {
        await using var check = db.CreateContext();
        var paper = await check.Ballots.CountAsync(b =>
            b.LocationGuid == seed.PaperLocationGuid);
        var onlineBallots = await check.Ballots.CountAsync(b =>
            b.ComputerCode == ComputerCodeHelper.Online);
        var processed = await check.OnlineVotingInfos.CountAsync(o =>
            o.ElectionGuid == seed.ElectionGuid
            && o.Status == OnlineBallotStatus.Processed);
        var pending = await check.OnlineVotingInfos.CountAsync(o =>
            o.ElectionGuid == seed.ElectionGuid
            && (o.Status == OnlineBallotStatus.Submitted
                || o.Status == OnlineBallotStatus.Processing));

        Assert.Equal(expectedPaper, paper);
        Assert.Equal(expectedOnlineBallots, onlineBallots);
        Assert.Equal(expectedPaper + expectedOnlineBallots, paper + onlineBallots);
        Assert.Equal(expectedProcessedOnline, processed);
        Assert.Equal(expectedPendingOnline, pending);
        Assert.Equal(
            expectedPaper + expectedOnlineBallots,
            await check.Ballots.CountAsync(b => b.Location.ElectionGuid == seed.ElectionGuid));
    }

    private static async Task<BallotDto> CreatePaperBallotAsync(
        MainDbContext context,
        SeededElection seed,
        string computerCode,
        string teller)
    {
        return await new BallotService(context, Mock.Of<ILogger<BallotService>>())
            .CreateBallotAsync(new CreateBallotDto
            {
                ElectionGuid = seed.ElectionGuid,
                LocationGuid = seed.PaperLocationGuid,
                ComputerCode = computerCode,
                Teller1 = teller
            });
    }

    private static async Task CreatePaperBallotsAsync(
        MainDbContext context,
        SeededElection seed,
        string computerCode,
        string teller,
        int count)
    {
        for (var i = 0; i < count; i++)
        {
            await CreatePaperBallotAsync(context, seed, computerCode, teller);
        }
    }

    private static Task<AcceptAllOnlineBallotsResultDto> AcceptAllAsync(
        MainDbContext context,
        Guid electionGuid) =>
        CreateOnlineService(context).AcceptAllPendingAsync(electionGuid);

    private static Task<FrontDeskVoterDto> CheckInAsync(
        MainDbContext context,
        SeededElection seed,
        SeededPerson person,
        string method) =>
        CreateFrontDesk(context).CheckInVoterAsync(seed.ElectionGuid, new CheckInVoterDto
        {
            PersonGuid = person.PersonGuid,
            VotingMethod = method,
            Teller1 = "Ada"
        });

    private static Task<FrontDeskVoterDto> UnregisterAsync(
        MainDbContext context,
        SeededElection seed,
        SeededPerson person) =>
        CreateFrontDesk(context).UnregisterVoterAsync(seed.ElectionGuid, new UnregisterVoterDto
        {
            PersonGuid = person.PersonGuid,
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

    private static async Task<SeededElection> SeedElectionAsync(
        SqliteConcurrentDb db,
        int submittedVoters)
    {
        await using var context = db.CreateContext();
        var electionGuid = Guid.NewGuid();
        var paperLocationGuid = Guid.NewGuid();
        context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Concurrent tellers #191",
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTimeOffset.UtcNow.AddHours(1),
            ElectionStage = ElectionStage.GatheringBallots,
            NumberToElect = 9,
            RowVersion = new byte[8]
        });
        context.Locations.Add(new Location
        {
            LocationGuid = paperLocationGuid,
            ElectionGuid = electionGuid,
            Name = "Hall"
        });

        var paperVoter = AddPerson(context, electionGuid, "Paper", "Voter");
        var submitted = new List<SeededPerson>();
        for (var i = 0; i < submittedVoters; i++)
        {
            submitted.Add(AddPerson(context, electionGuid, "Online", $"Voter{i}"));
        }

        await context.SaveChangesAsync();

        foreach (var person in submitted)
        {
            var submit = await CreateOnlineService(context).SubmitBallotAsync(new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = person.Email,
                Votes =
                [
                    new OnlineVoteDto
                    {
                        PersonGuid = person.PersonGuid,
                        VoteName = $"{person.FirstName} {person.LastName}",
                        PositionOnBallot = 1
                    }
                ]
            });
            Assert.True(submit.Success);
        }

        return new SeededElection(electionGuid, paperLocationGuid, paperVoter, submitted);
    }

    private static SeededPerson AddPerson(
        MainDbContext context,
        Guid electionGuid,
        string first,
        string last)
    {
        var personGuid = Guid.NewGuid();
        var email = $"{first.ToLowerInvariant()}_{Guid.NewGuid():N}@example.com";
        context.People.Add(new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = personGuid,
            FirstName = first,
            LastName = last,
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
        return new SeededPerson(personGuid, email, first, last);
    }

    private static Exception Unwrap(Exception ex) =>
        ex is AggregateException aggregate && aggregate.InnerExceptions.Count == 1
            ? aggregate.InnerExceptions[0]
            : ex;

    private sealed record SeededPerson(
        Guid PersonGuid,
        string Email,
        string FirstName,
        string LastName);

    private sealed record SeededElection(
        Guid ElectionGuid,
        Guid PaperLocationGuid,
        SeededPerson PaperVoter,
        IReadOnlyList<SeededPerson> Submitted);

    private sealed class AlwaysAllowAcceptLock : IOnlineBallotAcceptLock
    {
        public bool TryEnter(Guid electionGuid) => true;

        public void Exit(Guid electionGuid)
        {
        }
    }

    private sealed class SqliteConcurrentDb : IAsyncDisposable
    {
        private SqliteConcurrentDb(string dbPath, DbContextOptions<MainDbContext> options)
        {
            DbPath = dbPath;
            Options = options;
        }

        public string DbPath { get; }

        public DbContextOptions<MainDbContext> Options { get; }

        public static async Task<SqliteConcurrentDb> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"tallyj-191-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<MainDbContext>()
                .UseSqlite($"Data Source={path}", b => b.CommandTimeout(30))
                .Options;
            await using var context = new MainDbContext(options);
            await context.Database.EnsureCreatedAsync();
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            await context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");
            return new SqliteConcurrentDb(path, options);
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
