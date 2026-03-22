using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.Services.Analyzers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.UnitTests;

public class ElectionAnalyzerSingleNameTests : IDisposable
{
    private readonly MainDbContext _context;
    private Guid _electionGuid;
    private Guid _locationGuid;
    private List<Person> _samplePeople = new();

    public ElectionAnalyzerSingleNameTests()
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

    private Election CreateElection(int numberToElect, int numberExtra = 0)
    {
        _electionGuid = Guid.NewGuid();
        var election = new Election
        {
            ElectionGuid = _electionGuid,
            Name = "Test",
            NumberToElect = numberToElect,
            NumberExtra = numberExtra,
            ElectionType = "Oth",
            TallyStatus = "Setup",
            RowVersion = new byte[8]
        };
        _context.Elections.Add(election);

        _locationGuid = Guid.NewGuid();
        _context.Locations.Add(new Location
        {
            ElectionGuid = _electionGuid,
            LocationGuid = _locationGuid,
            Name = "Test Location"
        });

        _context.SaveChanges();
        return election;
    }

    private void CreateSamplePeople()
    {
        _samplePeople = new List<Person>
        {
            MakePerson("x0", votingMethod: "P"),
            MakePerson("x1"),
            MakePerson("x2"),
            MakePerson("x3"),
            MakePerson("x4"),
            MakePerson("x5"),
            MakePerson("x6"),
            MakePerson("x7"),
        };
        _context.SaveChanges();
    }

    private Person MakePerson(string firstName, string combinedInfo = "person",
        string? votingMethod = null, Guid? ineligibleReasonGuid = null,
        bool canReceiveVotes = true, bool canVote = true)
    {
        var person = new Person
        {
            ElectionGuid = _electionGuid,
            PersonGuid = Guid.NewGuid(),
            FirstName = firstName,
            LastName = firstName,
            CombinedInfo = combinedInfo,
            VotingMethod = votingMethod,
            IneligibleReasonGuid = ineligibleReasonGuid,
            CanReceiveVotes = canReceiveVotes,
            CanVote = canVote,
            RowVersion = new byte[8]
        };
        _context.People.Add(person);
        return person;
    }

    private Ballot MakeBallot(BallotStatus status = BallotStatus.Ok)
    {
        var ballot = new Ballot
        {
            LocationGuid = _locationGuid,
            BallotGuid = Guid.NewGuid(),
            StatusCode = status,
            ComputerCode = "A",
            BallotNumAtComputer = 1,
            RowVersion = new byte[8]
        };
        _context.Ballots.Add(ballot);
        return ballot;
    }

    private Vote MakeVote(Ballot ballot, Person person, int count = 0)
    {
        var vote = new Vote
        {
            BallotGuid = ballot.BallotGuid,
            PersonGuid = person.PersonGuid,
            PersonCombinedInfo = person.CombinedInfo,
            IneligibleReasonCode = person.IneligibleReasonGuid.HasValue
                ? IneligibleReasonEnum.GetByGuid(person.IneligibleReasonGuid)?.Code
                : null,
            SingleNameElectionCount = count,
            PositionOnBallot = 1,
            VoteStatus = VoteStatus.Ok,
            RowVersion = new byte[8]
        };
        _context.Votes.Add(vote);
        return vote;
    }

    private async Task<ElectionAnalyzerSingleName> RunAnalysis(Election election)
    {
        _context.SaveChanges();
        var logger = NullLogger.Instance;
        var analyzer = new ElectionAnalyzerSingleName(_context, logger, election);
        await analyzer.AnalyzeAsync();
        return analyzer;
    }

    [Fact]
    public async Task Election_3_people()
    {
        var election = CreateElection(numberToElect: 1);
        CreateSamplePeople();

        var ballots = new[] { MakeBallot(), MakeBallot(), MakeBallot() };

        MakeVote(ballots[0], _samplePeople[0], 33);
        MakeVote(ballots[1], _samplePeople[0], 5);
        MakeVote(ballots[2], _samplePeople[1], 2);

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();

        Assert.Equal(2, results.Count);

        Assert.Equal(38, results[0].VoteCount);
        Assert.Equal(1, results[0].Rank);
        Assert.Equal("E", results[0].Section);
        Assert.Equal(false, results[0].IsTied);

        Assert.Equal(2, results[1].VoteCount);
        Assert.Equal(2, results[1].Rank);
        Assert.Equal("O", results[1].Section);
        Assert.Equal(false, results[1].IsTied);
    }

    [Fact]
    public async Task Election_3_people_With_Manual_Results()
    {
        var election = CreateElection(numberToElect: 1);
        CreateSamplePeople();

        var ballots = new[] { MakeBallot(), MakeBallot(), MakeBallot() };

        MakeVote(ballots[0], _samplePeople[0], 33);
        MakeVote(ballots[1], _samplePeople[0], 5);
        MakeVote(ballots[2], _samplePeople[1], 2);

        _context.ResultSummaries.Add(new ResultSummary
        {
            ElectionGuid = _electionGuid,
            ResultType = "M",
            SpoiledManualBallots = 1
        });
        _context.SaveChanges();

        await RunAnalysis(election);

        var summaryFinal = _context.ResultSummaries
            .First(rs => rs.ElectionGuid == _electionGuid && rs.ResultType == "F");

        Assert.Equal(0, summaryFinal.BallotsNeedingReview);

        var numBallotsWithManual = (summaryFinal.BallotsReceived ?? 0) + (summaryFinal.SpoiledBallots ?? 0);
        Assert.Equal(41, numBallotsWithManual);

        Assert.Equal(0, summaryFinal.DroppedOffBallots);
        Assert.Equal(1, summaryFinal.InPersonBallots);
        Assert.Equal(0, summaryFinal.MailedInBallots);
        Assert.Equal(0, summaryFinal.CalledInBallots);
        Assert.Equal(0, summaryFinal.OnlineBallots);
        Assert.Equal(8, summaryFinal.NumEligibleToVote);
        Assert.Equal(40, summaryFinal.NumVoters);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal(38, results[0].VoteCount);
        Assert.Equal(2, results[1].VoteCount);
    }

    [Fact]
    public async Task Election_3_people_with_Tie()
    {
        var election = CreateElection(numberToElect: 1);
        CreateSamplePeople();

        var ballots = new[] { MakeBallot(), MakeBallot(), MakeBallot() };

        MakeVote(ballots[0], _samplePeople[0], 10);
        MakeVote(ballots[1], _samplePeople[1], 10);
        MakeVote(ballots[2], _samplePeople[2], 2);

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();
        var resultTies = _context.ResultTies
            .Where(rt => rt.ElectionGuid == _electionGuid)
            .OrderBy(rt => rt.TieBreakGroup).ToList();

        Assert.Single(resultTies);
        Assert.Equal(1, resultTies[0].NumToElect);
        Assert.Equal(2, resultTies[0].NumInTie);
        Assert.Equal(true, resultTies[0].TieBreakRequired);

        Assert.Equal(3, results.Count);

        Assert.Equal(10, results[0].VoteCount);
        Assert.Equal(1, results[0].Rank);
        Assert.Equal("E", results[0].Section);
        Assert.Equal(true, results[0].IsTied);
        Assert.Equal(1, results[0].TieBreakGroup);
        Assert.Equal(true, results[0].TieBreakRequired);

        Assert.Equal(10, results[1].VoteCount);
        Assert.Equal(2, results[1].Rank);
        Assert.Equal("O", results[1].Section);
        Assert.Equal(true, results[1].IsTied);
        Assert.Equal(1, results[1].TieBreakGroup);
        Assert.Equal(true, results[1].ForceShowInOther);
        Assert.Equal(true, results[1].TieBreakRequired);

        Assert.Equal(2, results[2].VoteCount);
        Assert.Equal(3, results[2].Rank);
        Assert.Equal("O", results[2].Section);
        Assert.Equal(false, results[2].IsTied);
        Assert.Null(results[2].TieBreakGroup);
        Assert.Equal(false, results[2].ForceShowInOther);
        Assert.Equal(false, results[2].TieBreakRequired);
    }

    [Fact]
    public async Task SingleNameElection_1_person()
    {
        var election = CreateElection(numberToElect: 1);
        CreateSamplePeople();

        var ballot = MakeBallot();
        MakeVote(ballot, _samplePeople[0], 33);
        MakeVote(ballot, _samplePeople[0], 5);
        MakeVote(ballot, _samplePeople[0], 2);

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();

        Assert.Single(results);

        Assert.Equal(40, results[0].VoteCount);
        Assert.Equal(1, results[0].Rank);
        Assert.Equal("E", results[0].Section);

        Assert.Equal(false, results[0].CloseToNext);
        Assert.Equal(false, results[0].CloseToPrev);
        Assert.Equal(false, results[0].ForceShowInOther);
        Assert.Null(results[0].IsTieResolved);
        Assert.Equal(false, results[0].IsTied);
        Assert.Null(results[0].RankInExtra);
        Assert.Null(results[0].TieBreakCount);
        Assert.Null(results[0].TieBreakGroup);
        Assert.Equal(false, results[0].TieBreakRequired);
    }

    [Fact]
    public async Task Invalid_Ballots_Affect_Results()
    {
        var election = CreateElection(numberToElect: 1);
        CreateSamplePeople();

        var ballots = new[]
        {
            MakeBallot(),
            MakeBallot(),
            MakeBallot(BallotStatus.TooFew)
        };

        MakeVote(ballots[0], _samplePeople[0], 33);
        MakeVote(ballots[0], _samplePeople[1], 5);
        MakeVote(ballots[0], _samplePeople[2], 2);
        MakeVote(ballots[1], _samplePeople[3], 4);
        MakeVote(ballots[0], _samplePeople[4], 27);

        var ineligiblePerson = MakePerson("z1",
            ineligibleReasonGuid: IneligibleReasonEnum.X09_OtherCannotVoteOrBeVotedFor.ReasonGuid,
            canReceiveVotes: false, canVote: false);
        _context.SaveChanges();

        var vote5 = MakeVote(ballots[0], _samplePeople[5], 27);
        vote5.PersonCombinedInfo = "different";

        MakeVote(ballots[0], ineligiblePerson, 27);

        await RunAnalysis(election);

        var updatedBallots = _context.Ballots
            .Where(b => b.LocationGuid == _locationGuid)
            .OrderBy(b => b.BallotGuid)
            .ToList();

        var ballot0 = updatedBallots.First(b => b.BallotGuid == ballots[0].BallotGuid);
        var ballot1 = updatedBallots.First(b => b.BallotGuid == ballots[1].BallotGuid);
        Assert.Equal(BallotStatus.Verify, ballot0.StatusCode);
        Assert.Equal(BallotStatus.Ok, ballot1.StatusCode);

        var summaryFinal = _context.ResultSummaries
            .First(rs => rs.ElectionGuid == _electionGuid && rs.ResultType == "F");
        Assert.Equal(1, summaryFinal.SpoiledBallots);
        Assert.Equal(1, summaryFinal.BallotsNeedingReview);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task Invalid_People_Do_Not_Affect_Results()
    {
        var election = CreateElection(numberToElect: 1);
        CreateSamplePeople();

        _samplePeople[5].IneligibleReasonGuid = IneligibleReasonEnum.X01_Deceased.ReasonGuid;
        _samplePeople[5].CanReceiveVotes = false;
        _samplePeople[5].CanVote = false;
        _context.SaveChanges();

        var ballots = new[] { MakeBallot(), MakeBallot(), MakeBallot() };

        var ineligiblePerson2 = MakePerson("z2",
            ineligibleReasonGuid: IneligibleReasonEnum.X09_OtherCannotVoteOrBeVotedFor.ReasonGuid,
            canReceiveVotes: false, canVote: false);
        _context.SaveChanges();

        MakeVote(ballots[0], _samplePeople[0], 33);
        MakeVote(ballots[0], _samplePeople[1], 5);
        MakeVote(ballots[0], _samplePeople[2], 5);
        MakeVote(ballots[0], _samplePeople[3], 5);

        MakeVote(ballots[1], _samplePeople[5], 27);
        MakeVote(ballots[1], ineligiblePerson2, 27);

        var vote6 = MakeVote(ballots[2], _samplePeople[4], 27);
        vote6.PersonCombinedInfo = "different";

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();
        var resultTies = _context.ResultTies
            .Where(rt => rt.ElectionGuid == _electionGuid)
            .OrderBy(rt => rt.TieBreakGroup).ToList();

        Assert.Single(resultTies);
        Assert.Equal(false, resultTies[0].TieBreakRequired);
        Assert.Equal(0, resultTies[0].NumToElect);
        Assert.Equal(3, resultTies[0].NumInTie);

        var summaryFinal = _context.ResultSummaries
            .First(rs => rs.ElectionGuid == _electionGuid && rs.ResultType == "F");
        Assert.Equal(1, summaryFinal.BallotsNeedingReview);
        Assert.Equal(1, summaryFinal.SpoiledBallots);
        Assert.Equal(54, summaryFinal.SpoiledVotes);

        Assert.Equal(4, results.Count);

        Assert.Equal(33, results[0].VoteCount);
        Assert.Equal("E", results[0].Section);
        Assert.Equal(false, results[0].IsTied);

        Assert.Equal(5, results[1].VoteCount);
        Assert.Equal("O", results[1].Section);
        Assert.Equal(true, results[1].IsTied);
        Assert.Equal(false, results[1].TieBreakRequired);

        Assert.Equal(5, results[2].VoteCount);
        Assert.Equal("O", results[2].Section);
        Assert.Equal(true, results[2].IsTied);
        Assert.Equal(false, results[2].TieBreakRequired);
    }

    [Fact]
    public async Task SingleNameElection_3_people()
    {
        var election = CreateElection(numberToElect: 1);
        CreateSamplePeople();

        var ballot = MakeBallot();
        MakeVote(ballot, _samplePeople[0], 33);
        MakeVote(ballot, _samplePeople[1], 5);
        MakeVote(ballot, _samplePeople[2], 2);

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();

        Assert.Equal(3, results.Count);

        Assert.Equal(33, results[0].VoteCount);
        Assert.Equal(1, results[0].Rank);
        Assert.Equal("E", results[0].Section);

        Assert.Equal(5, results[1].VoteCount);
        Assert.Equal(2, results[1].Rank);
        Assert.Equal("O", results[1].Section);
        Assert.Equal(false, results[1].IsTied);
    }

    [Fact]
    public async Task SingleNameElection_3_people_with_Tie()
    {
        var election = CreateElection(numberToElect: 1);
        CreateSamplePeople();

        var ballot = MakeBallot();
        MakeVote(ballot, _samplePeople[0], 10);
        MakeVote(ballot, _samplePeople[1], 10);
        MakeVote(ballot, _samplePeople[2], 2);

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();

        Assert.Equal(3, results.Count);

        Assert.Equal(10, results[0].VoteCount);
        Assert.Equal(1, results[0].Rank);
        Assert.Equal("E", results[0].Section);
        Assert.Equal(true, results[0].IsTied);
        Assert.Equal(1, results[0].TieBreakGroup);

        Assert.Equal(10, results[1].VoteCount);
        Assert.Equal(2, results[1].Rank);
        Assert.Equal("O", results[1].Section);
        Assert.Equal(true, results[1].IsTied);
        Assert.Equal(1, results[1].TieBreakGroup);
    }

    [Fact]
    public async Task SingleNameElection_3_people_with_3_way_Tie()
    {
        var election = CreateElection(numberToElect: 1);
        CreateSamplePeople();

        var ballot = MakeBallot();
        MakeVote(ballot, _samplePeople[0], 10);
        MakeVote(ballot, _samplePeople[1], 10);
        MakeVote(ballot, _samplePeople[2], 10);

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();

        Assert.Equal(3, results.Count);

        Assert.Equal(10, results[0].VoteCount);
        Assert.Equal(1, results[0].Rank);
        Assert.Equal("E", results[0].Section);
        Assert.Equal(true, results[0].IsTied);
        Assert.Equal(1, results[0].TieBreakGroup);

        Assert.Equal(10, results[1].VoteCount);
        Assert.Equal(2, results[1].Rank);
        Assert.Equal("O", results[1].Section);
        Assert.Equal(true, results[1].IsTied);
        Assert.Equal(1, results[1].TieBreakGroup);

        Assert.Equal(10, results[2].VoteCount);
        Assert.Equal(3, results[2].Rank);
        Assert.Equal("O", results[2].Section);
        Assert.Equal(true, results[2].IsTied);
        Assert.Equal(1, results[2].TieBreakGroup);
    }
}
