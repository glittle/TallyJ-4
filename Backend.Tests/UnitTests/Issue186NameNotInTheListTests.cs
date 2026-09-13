using System.Security.Claims;
using Backend.Context;
using Backend.DTOs.People;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;
using Backend.Services.Analyzers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Backend.Tests.UnitTests;

/// <summary>
/// #186 “Name not in the List”: adding a new eligible person is a valid vote
/// when guests are allowed (v3 GuestTellersCanAddPeople / GA).
/// </summary>
public class Issue186NameNotInTheListTests : IDisposable
{
    private readonly MainDbContext _context;

    public Issue186NameNotInTheListTests()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new MainDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task NewEligiblePersonVote_CountsInAnalyze()
    {
        var election = SeedElection();
        var listed = SeedPerson(election.ElectionGuid, "Listed", "Ada", canReceive: true);
        var writeIn = SeedPerson(election.ElectionGuid, "Newname", "Pat", canReceive: true);
        var location = _context.Locations.Single();
        var ballot = new Ballot
        {
            LocationGuid = location.LocationGuid,
            BallotGuid = Guid.NewGuid(),
            StatusCode = BallotStatus.Ok,
            ComputerCode = "A",
            BallotNumAtComputer = 1,
            RowVersion = new byte[8]
        };
        _context.Ballots.Add(ballot);
        _context.Votes.AddRange(
            VoteOn(ballot, listed, 1),
            VoteOn(ballot, writeIn, 2));
        await _context.SaveChangesAsync();

        await new ElectionAnalyzerNormal(_context, NullLogger.Instance, election).AnalyzeAsync();

        var writeInResult = _context.Results.Single(r => r.PersonGuid == writeIn.PersonGuid);
        Assert.Equal(1, writeInResult.VoteCount);
        Assert.Equal("E", writeInResult.Section);
        Assert.Equal(VoteStatus.Ok, _context.Votes.Single(v => v.PersonGuid == writeIn.PersonGuid).VoteStatus);
    }

    [Fact]
    public async Task NewIneligiblePersonVote_IsSpoiledAndDoesNotElect()
    {
        var election = SeedElection();
        var listed = SeedPerson(election.ElectionGuid, "Listed", "Ada", canReceive: true);
        var spoiled = SeedPerson(
            election.ElectionGuid,
            "Unknown",
            "Pat",
            canReceive: false,
            ineligibleReasonCode: IneligibleReasonEnum.X04_NotARegisteredBahai.Code);
        var location = _context.Locations.Single();
        var ballot = new Ballot
        {
            LocationGuid = location.LocationGuid,
            BallotGuid = Guid.NewGuid(),
            StatusCode = BallotStatus.Ok,
            ComputerCode = "A",
            BallotNumAtComputer = 1,
            RowVersion = new byte[8]
        };
        _context.Ballots.Add(ballot);
        _context.Votes.AddRange(
            VoteOn(ballot, listed, 1),
            new Vote
            {
                BallotGuid = ballot.BallotGuid,
                PersonGuid = spoiled.PersonGuid,
                PersonCombinedInfo = spoiled.CombinedInfo,
                PositionOnBallot = 2,
                VoteStatus = VoteStatus.Spoiled,
                IneligibleReasonCode = spoiled.IneligibleReasonCode,
                SingleNameElectionCount = 1
            });
        await _context.SaveChangesAsync();

        await new ElectionAnalyzerNormal(_context, NullLogger.Instance, election).AnalyzeAsync();

        Assert.DoesNotContain(
            _context.Results.Where(r => r.ElectionGuid == election.ElectionGuid),
            r => r.PersonGuid == spoiled.PersonGuid && r.Section == "E");
    }

    [Fact]
    public async Task GuestCreatePerson_WhenDisabled_Throws()
    {
        var election = SeedElection(guestCanAdd: false);
        var service = CreatePeopleService(guestTeller: true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreatePersonAsync(new CreatePersonDto
            {
                ElectionGuid = election.ElectionGuid,
                LastName = "Newname",
                FirstName = "Pat"
            }));

        Assert.Equal(PeopleMessageKeys.GuestCannotAddPeople, ex.Message);
        Assert.Empty(_context.People.Where(p => p.LastName == "Newname"));
    }

    [Fact]
    public async Task GuestCreatePerson_WhenEnabled_CreatesEligiblePerson()
    {
        var election = SeedElection(guestCanAdd: true);
        var service = CreatePeopleService(guestTeller: true);

        var person = await service.CreatePersonAsync(new CreatePersonDto
        {
            ElectionGuid = election.ElectionGuid,
            LastName = "Newname",
            FirstName = "Pat"
        });

        Assert.Equal("Newname", person.LastName);
        Assert.True(person.CanVote);
        Assert.True(person.CanReceiveVotes);
    }

    [Fact]
    public async Task FullTellerCreatePerson_WhenGuestAddDisabled_Succeeds()
    {
        var election = SeedElection(guestCanAdd: false);
        var service = CreatePeopleService(guestTeller: false);

        var person = await service.CreatePersonAsync(new CreatePersonDto
        {
            ElectionGuid = election.ElectionGuid,
            LastName = "Newname",
            FirstName = "Pat"
        });

        Assert.Equal("Newname", person.LastName);
    }

    private Election SeedElection(bool guestCanAdd = false)
    {
        var election = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Name not in the List",
            NumberToElect = 2,
            ElectionType = "LSA",
            ElectionStage = ElectionStage.ProcessingBallots,
            GuestTellersCanAddPeople = guestCanAdd,
            RowVersion = new byte[8]
        };
        _context.Elections.Add(election);
        _context.Locations.Add(new Location
        {
            ElectionGuid = election.ElectionGuid,
            LocationGuid = Guid.NewGuid(),
            Name = "Hall"
        });
        _context.SaveChanges();
        return election;
    }

    private Person SeedPerson(
        Guid electionGuid,
        string last,
        string first,
        bool canReceive,
        string? ineligibleReasonCode = null)
    {
        var person = new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            LastName = last,
            FirstName = first,
            CanVote = true,
            CanReceiveVotes = canReceive,
            IneligibleReasonCode = ineligibleReasonCode,
            CombinedInfo = $"{last}, {first}",
            RowVersion = new byte[8]
        };
        _context.People.Add(person);
        return person;
    }

    private static Vote VoteOn(Ballot ballot, Person person, int position) =>
        new()
        {
            BallotGuid = ballot.BallotGuid,
            PersonGuid = person.PersonGuid,
            PersonCombinedInfo = person.CombinedInfo,
            PositionOnBallot = position,
            VoteStatus = VoteStatus.Ok,
            SingleNameElectionCount = 1
        };

    private PeopleService CreatePeopleService(bool guestTeller)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        var identity = new ClaimsIdentity("AccessCode");
        if (guestTeller)
        {
            identity.AddClaim(new Claim(GuestTellerClaims.IsTellerClaimType, "true"));
        }

        accessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        });

        return new PeopleService(
            _context,
            Mock.Of<ILogger<PeopleService>>(),
            Mock.Of<ISignalRNotificationService>(),
            accessor.Object);
    }
}
