using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.Services.Analyzers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.UnitTests;

public class ElectionAnalyzerNormalTests : IDisposable
{
    private readonly MainDbContext _context;
    private Guid _electionGuid;
    private Guid _locationGuid;
    private List<Person> _samplePeople = new();

    public ElectionAnalyzerNormalTests()
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

    private Election CreateElection(int numberToElect, int numberExtra = 0, string electionType = "LSA")
    {
        _electionGuid = Guid.NewGuid();
        var election = new Election
        {
            ElectionGuid = _electionGuid,
            Name = "Test",
            NumberToElect = numberToElect,
            NumberExtra = numberExtra,
            ElectionType = electionType,
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
            MakePerson("a0", combinedInfo: "abc", votingMethod: "P"),
            MakePerson("a1"),
            MakePerson("a2"),
            MakePerson("a3"),
            MakePerson("a4"),
            MakePerson("a5", ineligibleReasonGuid: IneligibleReasonEnum.X02_MovedElsewhereRecently.ReasonGuid, canReceiveVotes: false, canVote: false),
            MakePerson("a6", ineligibleReasonGuid: IneligibleReasonEnum.V01_YouthAged181920.ReasonGuid, canReceiveVotes: false, canVote: true),
            MakePerson("a7", ineligibleReasonGuid: IneligibleReasonEnum.X06_ResidesElsewhere.ReasonGuid, canReceiveVotes: false, canVote: false),
        };
        _context.SaveChanges();
    }

    private Person MakePerson(string firstName, string combinedInfo = "abc",
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
            CombinedInfoAtStart = combinedInfo,
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

    private Vote MakeVote(Ballot ballot, Person person)
    {
        var vote = new Vote
        {
            BallotGuid = ballot.BallotGuid,
            PersonGuid = person.PersonGuid,
            PersonCombinedInfo = person.CombinedInfo,
            IneligibleReasonCode = person.IneligibleReasonGuid.HasValue
                ? IneligibleReasonEnum.GetByGuid(person.IneligibleReasonGuid)?.Code
                : null,
            PositionOnBallot = 1,
            VoteStatus = VoteStatus.Ok,
            RowVersion = new byte[8]
        };
        _context.Votes.Add(vote);
        return vote;
    }

    private Vote MakeVoteForIneligible(Ballot ballot, Guid ineligibleReasonGuid)
    {
        var reason = IneligibleReasonEnum.GetByGuid(ineligibleReasonGuid);
        var vote = new Vote
        {
            BallotGuid = ballot.BallotGuid,
            PersonGuid = null,
            IneligibleReasonCode = reason?.Code,
            PositionOnBallot = 1,
            VoteStatus = VoteStatus.Ok,
            RowVersion = new byte[8]
        };
        _context.Votes.Add(vote);
        return vote;
    }

    private async Task<ElectionAnalyzerNormal> RunAnalysis(Election election)
    {
        _context.SaveChanges();
        var logger = NullLogger.Instance;
        var analyzer = new ElectionAnalyzerNormal(_context, logger, election);
        await analyzer.AnalyzeAsync();
        return analyzer;
    }

    [Fact]
    public async Task Ballot_TwoPeople()
    {
        var election = CreateElection(numberToElect: 2);
        CreateSamplePeople();

        var ballot = MakeBallot();
        MakeVote(ballot, _samplePeople[0]);
        MakeVote(ballot, _samplePeople[1]);

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();

        Assert.Equal(2, results.Count);

        Assert.Equal(1, results[0].VoteCount);
        Assert.Equal(1, results[0].Rank);
        Assert.Equal(ResultSection.Elected, results[0].SectionCode);

        Assert.Equal(1, results[1].VoteCount);
        Assert.Equal(2, results[1].Rank);
        Assert.Equal(ResultSection.Elected, results[1].SectionCode);

        var summaryFinal = _context.ResultSummaries
            .First(rs => rs.ElectionGuid == _electionGuid && rs.ResultType == "F");

        Assert.Equal(0, summaryFinal.BallotsNeedingReview);
        Assert.Equal(0, summaryFinal.DroppedOffBallots);
        Assert.Equal(1, summaryFinal.InPersonBallots);
        Assert.Equal(0, summaryFinal.MailedInBallots);
        Assert.Equal(0, summaryFinal.CalledInBallots);
        Assert.Equal(0, summaryFinal.OnlineBallots);
        Assert.Equal(0, summaryFinal.ImportedBallots);
        Assert.Equal(0, summaryFinal.Custom1Ballots);
        Assert.Equal(0, summaryFinal.Custom2Ballots);
        Assert.Equal(0, summaryFinal.Custom3Ballots);
        Assert.Equal(6, summaryFinal.NumEligibleToVote);
        Assert.Equal(1, summaryFinal.NumVoters);
        Assert.Equal("F", summaryFinal.ResultType);

        var numBallotsWithManual = (summaryFinal.BallotsReceived ?? 0) + (summaryFinal.SpoiledBallots ?? 0);
        Assert.Equal(1, numBallotsWithManual);
    }

    [Fact]
    public async Task Ballot_TwoPeople_NameChanged()
    {
        var election = CreateElection(numberToElect: 2);
        CreateSamplePeople();

        var ballot = MakeBallot();
        var vote0 = MakeVote(ballot, _samplePeople[0]);
        MakeVote(ballot, _samplePeople[1]);

        vote0.PersonCombinedInfo = "very different";

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();

        Assert.Empty(results);

        var summaryFinal = _context.ResultSummaries
            .First(rs => rs.ElectionGuid == _electionGuid && rs.ResultType == "F");

        Assert.Equal(1, summaryFinal.BallotsNeedingReview);

        var numBallotsWithManual = (summaryFinal.BallotsReceived ?? 0) + (summaryFinal.SpoiledBallots ?? 0);
        Assert.Equal(1, numBallotsWithManual);

        Assert.Equal(0, summaryFinal.DroppedOffBallots);
        Assert.Equal(1, summaryFinal.InPersonBallots);
        Assert.Equal(0, summaryFinal.MailedInBallots);
        Assert.Equal(0, summaryFinal.CalledInBallots);
        Assert.Equal(0, summaryFinal.OnlineBallots);
        Assert.Equal(6, summaryFinal.NumEligibleToVote);
        Assert.Equal(1, summaryFinal.NumVoters);
    }

    [Fact]
    public async Task Ballot_TwoPeople_NameExtended()
    {
        var election = CreateElection(numberToElect: 2);
        CreateSamplePeople();

        var ballot = MakeBallot();
        var vote0 = MakeVote(ballot, _samplePeople[0]);
        MakeVote(ballot, _samplePeople[1]);

        vote0.PersonCombinedInfo = "ab";

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();

        Assert.Equal(2, results.Count);

        var summaryFinal = _context.ResultSummaries
            .First(rs => rs.ElectionGuid == _electionGuid && rs.ResultType == "F");

        Assert.Equal(0, summaryFinal.BallotsNeedingReview);
        Assert.Equal(6, summaryFinal.NumEligibleToVote);
        Assert.Equal(1, summaryFinal.NumVoters);
    }

    [Fact]
    public async Task Ballot_TwoNames_AllSpoiled()
    {
        var election = CreateElection(numberToElect: 2);
        CreateSamplePeople();

        var ballot = MakeBallot();
        MakeVoteForIneligible(ballot, IneligibleReasonEnum.U01_Unidentifiable.ReasonGuid);
        MakeVoteForIneligible(ballot, IneligibleReasonEnum.U01_Unidentifiable.ReasonGuid);

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid).ToList();
        Assert.Empty(results);

        var updatedBallot = _context.Ballots.First(b => b.BallotGuid == ballot.BallotGuid);
        Assert.Equal(BallotStatus.Ok, updatedBallot.StatusCode);

        var votes = _context.Votes.Where(v => v.BallotGuid == ballot.BallotGuid).ToList();
        Assert.All(votes, v => Assert.Equal(VoteStatus.Spoiled, v.VoteStatus));

        var summaryFinal = _context.ResultSummaries
            .First(rs => rs.ElectionGuid == _electionGuid && rs.ResultType == "F");
        Assert.Equal(0, summaryFinal.BallotsNeedingReview);
    }

    [Fact]
    public async Task Ballot_OlderYouth()
    {
        var election = CreateElection(numberToElect: 2);
        CreateSamplePeople();

        var ballot = MakeBallot();
        MakeVoteForIneligible(ballot, IneligibleReasonEnum.V01_YouthAged181920.ReasonGuid);
        MakeVote(ballot, _samplePeople[6]);

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid).ToList();
        Assert.Empty(results);

        var updatedBallot = _context.Ballots.First(b => b.BallotGuid == ballot.BallotGuid);
        Assert.Equal(BallotStatus.Ok, updatedBallot.StatusCode);

        var votes = _context.Votes.Where(v => v.BallotGuid == ballot.BallotGuid).ToList();
        Assert.All(votes, v => Assert.Equal(VoteStatus.Spoiled, v.VoteStatus));
    }

    [Fact]
    public async Task Ballot_TwoPeople_AllSpoiled()
    {
        var election = CreateElection(numberToElect: 2);
        CreateSamplePeople();

        var ballot = MakeBallot();
        MakeVote(ballot, _samplePeople[6]);
        MakeVote(ballot, _samplePeople[7]);

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid).ToList();
        Assert.Empty(results);

        var updatedBallot = _context.Ballots.First(b => b.BallotGuid == ballot.BallotGuid);
        Assert.Equal(BallotStatus.Ok, updatedBallot.StatusCode);

        var votes = _context.Votes.Where(v => v.BallotGuid == ballot.BallotGuid).ToList();
        Assert.All(votes, v => Assert.Equal(VoteStatus.Spoiled, v.VoteStatus));
    }

    [Fact]
    public async Task Election_3_people_with_Tie_Not_Required()
    {
        var election = CreateElection(numberToElect: 3);
        CreateSamplePeople();

        var ballots = new[] { MakeBallot(), MakeBallot(), MakeBallot() };

        MakeVote(ballots[0], _samplePeople[0]);
        MakeVote(ballots[1], _samplePeople[0]);
        MakeVote(ballots[2], _samplePeople[0]);
        MakeVote(ballots[0], _samplePeople[1]);
        MakeVote(ballots[1], _samplePeople[1]);
        MakeVote(ballots[2], _samplePeople[1]);
        MakeVote(ballots[0], _samplePeople[2]);
        MakeVote(ballots[1], _samplePeople[2]);
        MakeVote(ballots[2], _samplePeople[2]);

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();
        var resultTies = _context.ResultTies
            .Where(rt => rt.ElectionGuid == _electionGuid)
            .OrderBy(rt => rt.TieBreakGroup).ToList();

        Assert.Single(resultTies);
        Assert.Equal(0, resultTies[0].NumToElect);
        Assert.Equal(3, resultTies[0].NumInTie);
        Assert.Equal(false, resultTies[0].TieBreakRequired);

        Assert.Equal(3, results.Count);

        Assert.Equal(3, results[0].VoteCount);
        Assert.Equal(1, results[0].Rank);
        Assert.Equal(ResultSection.Elected, results[0].SectionCode);
        Assert.Equal(true, results[0].IsTied);
        Assert.Equal(1, results[0].TieBreakGroup);
        Assert.Equal(false, results[0].TieBreakRequired);
        Assert.Equal(false, results[0].ForceShowInOther);

        Assert.Equal(3, results[1].VoteCount);
        Assert.Equal(2, results[1].Rank);
        Assert.Equal(ResultSection.Elected, results[1].SectionCode);
        Assert.Equal(true, results[1].IsTied);
        Assert.Equal(1, results[1].TieBreakGroup);
        Assert.Equal(false, results[1].TieBreakRequired);
        Assert.Equal(false, results[1].ForceShowInOther);

        Assert.Equal(3, results[2].VoteCount);
        Assert.Equal(3, results[2].Rank);
        Assert.Equal(ResultSection.Elected, results[2].SectionCode);
        Assert.Equal(true, results[2].IsTied);
        Assert.Equal(1, results[2].TieBreakGroup);
        Assert.Equal(false, results[2].TieBreakRequired);
        Assert.Equal(false, results[2].ForceShowInOther);
    }

    [Fact]
    public async Task Election_3_people_with_3_way_Tie()
    {
        var election = CreateElection(numberToElect: 1);
        CreateSamplePeople();

        var ballots = new[] { MakeBallot(), MakeBallot(), MakeBallot() };

        MakeVote(ballots[0], _samplePeople[0]);
        MakeVote(ballots[1], _samplePeople[1]);
        MakeVote(ballots[2], _samplePeople[2]);

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();
        var resultTies = _context.ResultTies
            .Where(rt => rt.ElectionGuid == _electionGuid)
            .OrderBy(rt => rt.TieBreakGroup).ToList();

        Assert.Single(resultTies);
        Assert.Equal(1, resultTies[0].NumToElect);
        Assert.Equal(3, resultTies[0].NumInTie);
        Assert.Equal(true, resultTies[0].TieBreakRequired);

        Assert.Equal(3, results.Count);

        Assert.Equal(1, results[0].VoteCount);
        Assert.Equal(1, results[0].Rank);
        Assert.Equal(ResultSection.Elected, results[0].SectionCode);
        Assert.Equal(true, results[0].IsTied);
        Assert.Equal(1, results[0].TieBreakGroup);
        Assert.Equal(true, results[0].TieBreakRequired);

        Assert.Equal(1, results[1].VoteCount);
        Assert.Equal(2, results[1].Rank);
        Assert.Equal(ResultSection.Other, results[1].SectionCode);
        Assert.Equal(true, results[1].IsTied);
        Assert.Equal(1, results[1].TieBreakGroup);
        Assert.Equal(true, results[1].TieBreakRequired);
        Assert.Equal(true, results[1].ForceShowInOther);

        Assert.Equal(1, results[2].VoteCount);
        Assert.Equal(3, results[2].Rank);
        Assert.Equal(ResultSection.Other, results[2].SectionCode);
        Assert.Equal(true, results[2].IsTied);
        Assert.Equal(1, results[2].TieBreakGroup);
        Assert.Equal(true, results[2].TieBreakRequired);
        Assert.Equal(true, results[2].ForceShowInOther);
    }

    [Fact]
    public async Task ElectionWithTwoSetsOfTies()
    {
        var election = CreateElection(numberToElect: 2, numberExtra: 2);
        CreateSamplePeople();

        var ballots = new[] { MakeBallot(), MakeBallot(), MakeBallot(), MakeBallot(), MakeBallot() };

        MakeVote(ballots[0], _samplePeople[0]);
        MakeVote(ballots[0], _samplePeople[1]);
        MakeVote(ballots[1], _samplePeople[0]);
        MakeVote(ballots[1], _samplePeople[1]);
        MakeVote(ballots[2], _samplePeople[0]);
        MakeVote(ballots[2], _samplePeople[2]);
        MakeVote(ballots[3], _samplePeople[2]);
        MakeVote(ballots[3], _samplePeople[3]);
        MakeVote(ballots[4], _samplePeople[4]);
        MakeVote(ballots[4], _samplePeople[5]);

        await RunAnalysis(election);

        var summaryFinal = _context.ResultSummaries
            .First(rs => rs.ElectionGuid == _electionGuid && rs.ResultType == "F");
        var numBallotsWithManual = (summaryFinal.BallotsReceived ?? 0) + (summaryFinal.SpoiledBallots ?? 0);
        Assert.Equal(5, numBallotsWithManual);
        Assert.Equal(0, summaryFinal.SpoiledBallots);
        Assert.Equal(1, summaryFinal.SpoiledVotes);
        Assert.Equal(0, summaryFinal.BallotsNeedingReview);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();
        var resultTies = _context.ResultTies
            .Where(rt => rt.ElectionGuid == _electionGuid)
            .OrderBy(rt => rt.TieBreakGroup).ToList();

        Assert.Equal(2, resultTies.Count);
        Assert.Equal(1, resultTies[0].NumToElect);
        Assert.Equal(2, resultTies[0].NumInTie);
        Assert.Equal(true, resultTies[0].TieBreakRequired);

        Assert.Equal(1, resultTies[1].NumToElect);
        Assert.Equal(2, resultTies[1].NumInTie);
        Assert.Equal(true, resultTies[1].TieBreakRequired);

        Assert.Equal(5, results.Count);

        Assert.Equal(false, results[0].IsTied);
        Assert.Equal(false, results[0].CloseToPrev);
        Assert.Equal(true, results[0].CloseToNext);
        Assert.Equal(ResultSection.Elected, results[0].SectionCode);

        Assert.Equal(true, results[1].IsTied);
        Assert.Equal(1, results[1].TieBreakGroup);
        Assert.Equal(true, results[1].CloseToPrev);
        Assert.Equal(true, results[1].CloseToNext);
        Assert.Equal(ResultSection.Elected, results[1].SectionCode);
        Assert.Equal(true, results[1].TieBreakRequired);
        Assert.Equal(false, results[1].ForceShowInOther);

        Assert.Equal(true, results[2].IsTied);
        Assert.Equal(1, results[2].TieBreakGroup);
        Assert.Equal(true, results[2].CloseToPrev);
        Assert.Equal(true, results[2].CloseToNext);
        Assert.Equal(ResultSection.Extra, results[2].SectionCode);
        Assert.Equal(true, results[2].TieBreakRequired);
        Assert.Equal(false, results[2].ForceShowInOther);

        Assert.Equal(true, results[3].IsTied);
        Assert.Equal(2, results[3].TieBreakGroup);
        Assert.Equal(true, results[3].CloseToPrev);
        Assert.Equal(true, results[3].CloseToNext);
        Assert.Equal(ResultSection.Extra, results[3].SectionCode);
        Assert.Equal(true, results[3].TieBreakRequired);
        Assert.Equal(false, results[3].ForceShowInOther);

        Assert.Equal(true, results[4].IsTied);
        Assert.Equal(2, results[4].TieBreakGroup);
        Assert.Equal(true, results[4].CloseToPrev);
        Assert.Equal(false, results[4].CloseToNext);
        Assert.Equal(ResultSection.Other, results[4].SectionCode);
        Assert.Equal(true, results[4].ForceShowInOther);
        Assert.Equal(true, results[4].TieBreakRequired);
    }

    [Fact]
    public async Task ElectionTieSpanningTopExtraOther()
    {
        var election = CreateElection(numberToElect: 2, numberExtra: 2);
        CreateSamplePeople();

        var ballots = new[] { MakeBallot(), MakeBallot(), MakeBallot() };

        MakeVote(ballots[0], _samplePeople[0]);
        MakeVote(ballots[0], _samplePeople[1]);
        MakeVote(ballots[1], _samplePeople[0]);
        MakeVote(ballots[1], _samplePeople[2]);
        MakeVote(ballots[2], _samplePeople[3]);
        MakeVote(ballots[2], _samplePeople[4]);

        await RunAnalysis(election);

        var summaryFinal = _context.ResultSummaries
            .First(rs => rs.ElectionGuid == _electionGuid && rs.ResultType == "F");
        var numBallotsWithManual = (summaryFinal.BallotsReceived ?? 0) + (summaryFinal.SpoiledBallots ?? 0);
        Assert.Equal(3, numBallotsWithManual);
        Assert.Equal(0, summaryFinal.SpoiledBallots);
        Assert.Equal(0, summaryFinal.SpoiledVotes);
        Assert.Equal(0, summaryFinal.BallotsNeedingReview);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();
        var resultTies = _context.ResultTies
            .Where(rt => rt.ElectionGuid == _electionGuid)
            .OrderBy(rt => rt.TieBreakGroup).ToList();

        Assert.Single(resultTies);
        Assert.Equal(1, resultTies[0].NumToElect);
        Assert.Equal(4, resultTies[0].NumInTie);
        Assert.Equal(true, resultTies[0].TieBreakRequired);

        Assert.Equal(5, results.Count);

        Assert.Equal(false, results[0].IsTied);
        Assert.Equal(false, results[0].CloseToPrev);
        Assert.Equal(true, results[0].CloseToNext);
        Assert.Equal(ResultSection.Elected, results[0].SectionCode);
        Assert.Equal(false, results[0].TieBreakRequired);
        Assert.Equal(false, results[0].ForceShowInOther);

        Assert.Equal(true, results[1].IsTied);
        Assert.Equal(1, results[1].TieBreakGroup);
        Assert.Equal(true, results[1].CloseToPrev);
        Assert.Equal(true, results[1].CloseToNext);
        Assert.Equal(ResultSection.Elected, results[1].SectionCode);
        Assert.Equal(true, results[1].TieBreakRequired);
        Assert.Equal(false, results[1].ForceShowInOther);

        Assert.Equal(true, results[2].IsTied);
        Assert.Equal(1, results[2].TieBreakGroup);
        Assert.Equal(true, results[2].CloseToPrev);
        Assert.Equal(true, results[2].CloseToNext);
        Assert.Equal(ResultSection.Extra, results[2].SectionCode);
        Assert.Equal(true, results[2].TieBreakRequired);
        Assert.Equal(false, results[2].ForceShowInOther);

        Assert.Equal(true, results[3].IsTied);
        Assert.Equal(1, results[3].TieBreakGroup);
        Assert.Equal(true, results[3].CloseToPrev);
        Assert.Equal(true, results[3].CloseToNext);
        Assert.Equal(ResultSection.Extra, results[3].SectionCode);
        Assert.Equal(true, results[3].TieBreakRequired);
        Assert.Equal(false, results[3].ForceShowInOther);

        Assert.Equal(true, results[4].IsTied);
        Assert.Equal(1, results[4].TieBreakGroup);
        Assert.Equal(true, results[4].CloseToPrev);
        Assert.Equal(false, results[4].CloseToNext);
        Assert.Equal(ResultSection.Other, results[4].SectionCode);
        Assert.Equal(true, results[4].ForceShowInOther);
        Assert.Equal(true, results[4].TieBreakRequired);
    }

    [Fact]
    public async Task ElectionTieWithTieBreakTiedInTopSection()
    {
        var election = CreateElection(numberToElect: 2, numberExtra: 2);
        CreateSamplePeople();

        var ballots = new[] { MakeBallot(), MakeBallot() };

        MakeVote(ballots[0], _samplePeople[0]);
        MakeVote(ballots[0], _samplePeople[1]);
        MakeVote(ballots[1], _samplePeople[2]);
        MakeVoteForIneligible(ballots[1], IneligibleReasonEnum.U01_Unidentifiable.ReasonGuid);

        await RunAnalysis(election);

        var summaryFinal = _context.ResultSummaries
            .First(rs => rs.ElectionGuid == _electionGuid && rs.ResultType == "F");
        var numBallotsWithManual = (summaryFinal.BallotsReceived ?? 0) + (summaryFinal.SpoiledBallots ?? 0);
        Assert.Equal(2, numBallotsWithManual);
        Assert.Equal(0, summaryFinal.SpoiledBallots);
        Assert.Equal(1, summaryFinal.SpoiledVotes);
        Assert.Equal(0, summaryFinal.BallotsNeedingReview);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();
        Assert.Equal(3, results.Count);

        var resultTies = _context.ResultTies
            .Where(rt => rt.ElectionGuid == _electionGuid)
            .OrderBy(rt => rt.TieBreakGroup).ToList();
        Assert.Single(resultTies);
        Assert.Equal(2, resultTies[0].NumToElect);
        Assert.Equal(3, resultTies[0].NumInTie);
        Assert.Equal(true, resultTies[0].TieBreakRequired);

        Assert.Equal(true, results[0].IsTied);
        Assert.Equal(1, results[0].TieBreakGroup);
        Assert.Equal(false, results[0].CloseToPrev);
        Assert.Equal(true, results[0].CloseToNext);
        Assert.Equal(ResultSection.Elected, results[0].SectionCode);
        Assert.Equal(true, results[0].TieBreakRequired);
        Assert.Equal(false, results[0].ForceShowInOther);

        Assert.Equal(true, results[1].IsTied);
        Assert.Equal(1, results[1].TieBreakGroup);
        Assert.Equal(true, results[1].CloseToPrev);
        Assert.Equal(true, results[1].CloseToNext);
        Assert.Equal(ResultSection.Elected, results[1].SectionCode);
        Assert.Equal(true, results[1].TieBreakRequired);
        Assert.Equal(false, results[1].ForceShowInOther);

        Assert.Equal(true, results[2].IsTied);
        Assert.Equal(1, results[2].TieBreakGroup);
        Assert.Equal(true, results[2].CloseToPrev);
        Assert.Equal(false, results[2].CloseToNext);
        Assert.Equal(ResultSection.Extra, results[2].SectionCode);
        Assert.Equal(true, results[2].TieBreakRequired);
        Assert.Equal(false, results[2].ForceShowInOther);

        results[0].TieBreakCount = 1;
        results[1].TieBreakCount = 1;
        results[2].TieBreakCount = 0;
        _context.SaveChanges();

        await RunAnalysis(election);

        results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();
        Assert.Equal(3, results.Count);

        resultTies = _context.ResultTies
            .Where(rt => rt.ElectionGuid == _electionGuid)
            .OrderBy(rt => rt.TieBreakGroup).ToList();
        Assert.Single(resultTies);
        Assert.Equal(2, resultTies[0].NumToElect);
        Assert.Equal(3, resultTies[0].NumInTie);
        Assert.Equal(true, resultTies[0].TieBreakRequired);
        Assert.Equal(true, resultTies[0].IsResolved);

        Assert.Equal(true, results[0].IsTied);
        Assert.Equal(ResultSection.Elected, results[0].SectionCode);
        Assert.Equal(true, results[0].TieBreakRequired);
        Assert.Equal(true, results[0].IsTieResolved);

        Assert.Equal(true, results[1].IsTied);
        Assert.Equal(ResultSection.Elected, results[1].SectionCode);
        Assert.Equal(true, results[1].TieBreakRequired);
        Assert.Equal(true, results[1].IsTieResolved);

        Assert.Equal(true, results[2].IsTied);
        Assert.Equal(ResultSection.Extra, results[2].SectionCode);
        Assert.Equal(true, results[2].TieBreakRequired);
        Assert.Equal(true, results[2].IsTieResolved);
    }

    [Fact]
    public async Task ElectionTieWithTieBreakTiedInExtraSection()
    {
        var election = CreateElection(numberToElect: 2, numberExtra: 2);
        CreateSamplePeople();

        var ballots = new[] { MakeBallot(), MakeBallot() };

        MakeVote(ballots[0], _samplePeople[0]);
        MakeVote(ballots[0], _samplePeople[1]);
        MakeVote(ballots[1], _samplePeople[2]);
        MakeVoteForIneligible(ballots[1], IneligibleReasonEnum.U01_Unidentifiable.ReasonGuid);

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();
        Assert.Equal(3, results.Count);

        var resultTies = _context.ResultTies
            .Where(rt => rt.ElectionGuid == _electionGuid)
            .OrderBy(rt => rt.TieBreakGroup).ToList();
        Assert.Single(resultTies);
        Assert.Equal(2, resultTies[0].NumToElect);
        Assert.Equal(3, resultTies[0].NumInTie);
        Assert.Equal(true, resultTies[0].TieBreakRequired);

        results[0].TieBreakCount = 2;
        results[1].TieBreakCount = 1;
        results[2].TieBreakCount = 1;
        _context.SaveChanges();

        await RunAnalysis(election);

        results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();
        Assert.Equal(3, results.Count);

        resultTies = _context.ResultTies
            .Where(rt => rt.ElectionGuid == _electionGuid)
            .OrderBy(rt => rt.TieBreakGroup).ToList();
        Assert.Single(resultTies);
        Assert.Equal(2, resultTies[0].NumToElect);
        Assert.Equal(3, resultTies[0].NumInTie);
        Assert.Equal(true, resultTies[0].TieBreakRequired);
        Assert.Equal(false, resultTies[0].IsResolved);

        Assert.Equal(true, results[0].IsTied);
        Assert.Equal(ResultSection.Elected, results[0].SectionCode);
        Assert.Equal(true, results[0].TieBreakRequired);
        Assert.Equal(false, results[0].IsTieResolved);

        Assert.Equal(true, results[1].IsTied);
        Assert.Equal(ResultSection.Elected, results[1].SectionCode);
        Assert.Equal(true, results[1].TieBreakRequired);
        Assert.Equal(false, results[1].IsTieResolved);

        Assert.Equal(true, results[2].IsTied);
        Assert.Equal(ResultSection.Extra, results[2].SectionCode);
        Assert.Equal(true, results[2].TieBreakRequired);
        Assert.Equal(false, results[2].IsTieResolved);
    }

    [Fact]
    public async Task ElectionTieWithTieBreakTiedInExtraSection2()
    {
        var election = CreateElection(numberToElect: 2, numberExtra: 2);
        CreateSamplePeople();

        var ballots = new[] { MakeBallot(), MakeBallot(), MakeBallot() };

        MakeVote(ballots[0], _samplePeople[0]);
        MakeVote(ballots[0], _samplePeople[1]);
        MakeVote(ballots[1], _samplePeople[0]);
        MakeVote(ballots[1], _samplePeople[2]);
        MakeVote(ballots[2], _samplePeople[3]);
        MakeVoteForIneligible(ballots[2], IneligibleReasonEnum.X09_OtherCannotVoteOrBeVotedFor.ReasonGuid);

        await RunAnalysis(election);

        var summaryFinal = _context.ResultSummaries
            .First(rs => rs.ElectionGuid == _electionGuid && rs.ResultType == "F");
        var numBallotsWithManual = (summaryFinal.BallotsReceived ?? 0) + (summaryFinal.SpoiledBallots ?? 0);
        Assert.Equal(3, numBallotsWithManual);
        Assert.Equal(0, summaryFinal.SpoiledBallots);
        Assert.Equal(1, summaryFinal.SpoiledVotes);
        Assert.Equal(0, summaryFinal.BallotsNeedingReview);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();
        Assert.Equal(4, results.Count);

        var resultTies = _context.ResultTies
            .Where(rt => rt.ElectionGuid == _electionGuid)
            .OrderBy(rt => rt.TieBreakGroup).ToList();
        Assert.Single(resultTies);
        Assert.Equal(1, resultTies[0].NumToElect);
        Assert.Equal(3, resultTies[0].NumInTie);
        Assert.Equal(true, resultTies[0].TieBreakRequired);

        Assert.Equal(false, results[0].IsTied);
        Assert.Equal(false, results[0].CloseToPrev);
        Assert.Equal(true, results[0].CloseToNext);
        Assert.Equal(ResultSection.Elected, results[0].SectionCode);
        Assert.Equal(false, results[0].TieBreakRequired);
        Assert.Equal(false, results[0].ForceShowInOther);

        Assert.Equal(true, results[1].IsTied);
        Assert.Equal(1, results[1].TieBreakGroup);
        Assert.Equal(true, results[1].CloseToPrev);
        Assert.Equal(true, results[1].CloseToNext);
        Assert.Equal(ResultSection.Elected, results[1].SectionCode);
        Assert.Equal(true, results[1].TieBreakRequired);
        Assert.Equal(false, results[1].ForceShowInOther);

        Assert.Equal(true, results[2].IsTied);
        Assert.Equal(1, results[2].TieBreakGroup);
        Assert.Equal(true, results[2].CloseToPrev);
        Assert.Equal(true, results[2].CloseToNext);
        Assert.Equal(ResultSection.Extra, results[2].SectionCode);
        Assert.Equal(true, results[2].TieBreakRequired);
        Assert.Equal(false, results[2].ForceShowInOther);

        Assert.Equal(true, results[3].IsTied);
        Assert.Equal(1, results[3].TieBreakGroup);
        Assert.Equal(true, results[3].CloseToPrev);
        Assert.Equal(false, results[3].CloseToNext);
        Assert.Equal(ResultSection.Extra, results[3].SectionCode);
        Assert.Equal(true, results[3].TieBreakRequired);
        Assert.Equal(false, results[3].ForceShowInOther);

        results[1].TieBreakCount = 2;
        results[2].TieBreakCount = 1;
        results[3].TieBreakCount = 1;
        _context.SaveChanges();

        await RunAnalysis(election);

        results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();
        Assert.Equal(4, results.Count);

        resultTies = _context.ResultTies
            .Where(rt => rt.ElectionGuid == _electionGuid)
            .OrderBy(rt => rt.TieBreakGroup).ToList();
        Assert.Single(resultTies);
        Assert.Equal(1, resultTies[0].NumToElect);
        Assert.Equal(3, resultTies[0].NumInTie);
        Assert.Equal(true, resultTies[0].TieBreakRequired);
        Assert.Equal(false, resultTies[0].IsResolved);

        Assert.Equal(false, results[0].IsTied);
        Assert.Equal(ResultSection.Elected, results[0].SectionCode);

        Assert.Equal(true, results[1].IsTied);
        Assert.Equal(ResultSection.Elected, results[1].SectionCode);
        Assert.Equal(true, results[1].TieBreakRequired);

        Assert.Equal(true, results[2].IsTied);
        Assert.Equal(ResultSection.Extra, results[2].SectionCode);
        Assert.Equal(true, results[2].TieBreakRequired);

        Assert.Equal(true, results[3].IsTied);
        Assert.Equal(ResultSection.Extra, results[3].SectionCode);
        Assert.Equal(true, results[3].TieBreakRequired);
    }

    [Fact]
    public async Task NSA_Election_1()
    {
        var election = CreateElection(numberToElect: 2, electionType: "NSA");
        CreateSamplePeople();

        var ballots = new[] { MakeBallot(), MakeBallot(), MakeBallot() };

        MakeVote(ballots[0], _samplePeople[0]);
        MakeVote(ballots[0], _samplePeople[1]);
        MakeVote(ballots[1], _samplePeople[0]);
        MakeVote(ballots[1], _samplePeople[1]);
        MakeVote(ballots[2], _samplePeople[0]);
        MakeVote(ballots[2], _samplePeople[2]);

        await RunAnalysis(election);

        var results = _context.Results
            .Where(r => r.ElectionGuid == _electionGuid)
            .OrderBy(r => r.Rank).ToList();

        Assert.Equal(3, results.Count);

        Assert.Equal(3, results[0].VoteCount);
        Assert.Equal(1, results[0].Rank);
        Assert.Equal(ResultSection.Elected, results[0].SectionCode);

        Assert.Equal(2, results[1].VoteCount);
        Assert.Equal(2, results[1].Rank);
        Assert.Equal(ResultSection.Elected, results[1].SectionCode);

        Assert.Equal(1, results[2].VoteCount);
        Assert.Equal(3, results[2].Rank);
        Assert.Equal(ResultSection.Other, results[2].SectionCode);

        var summaryFinal = _context.ResultSummaries
            .First(rs => rs.ElectionGuid == _electionGuid && rs.ResultType == "F");
        Assert.Equal(0, summaryFinal.BallotsNeedingReview);
        var numBallotsWithManual = (summaryFinal.BallotsReceived ?? 0) + (summaryFinal.SpoiledBallots ?? 0);
        Assert.Equal(3, numBallotsWithManual);
        Assert.Equal(0, summaryFinal.DroppedOffBallots);
        Assert.Equal(1, summaryFinal.InPersonBallots);
        Assert.Equal(0, summaryFinal.MailedInBallots);
        Assert.Equal(0, summaryFinal.CalledInBallots);
        Assert.Equal(0, summaryFinal.OnlineBallots);
        Assert.Equal(6, summaryFinal.NumEligibleToVote);
        Assert.Equal(1, summaryFinal.NumVoters);
    }
}
