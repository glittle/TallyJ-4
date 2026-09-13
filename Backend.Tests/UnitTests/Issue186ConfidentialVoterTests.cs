using Backend.Context;
using Backend.DTOs.Results;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;
using Backend.Services.Analyzers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Backend.Tests.UnitTests;

/// <summary>
/// #186 confidential voter workflow: add as “Confidential X”, then bump
/// Eligible Voters with the v3 Analyze manual override (ResultType M).
/// </summary>
public class Issue186ConfidentialVoterTests : IDisposable
{
    private readonly MainDbContext _context;

    public Issue186ConfidentialVoterTests()
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
    public async Task AddConfidential1_IncreasesCalculatedEligibleVoters()
    {
        var election = SeedElection();
        SeedPerson(election.ElectionGuid, "Ada", "List", canVote: true);
        await _context.SaveChangesAsync();

        await new ElectionAnalyzerNormal(_context, NullLogger.Instance, election).AnalyzeAsync();
        var before = _context.ResultSummaries.Single(rs =>
            rs.ElectionGuid == election.ElectionGuid && rs.ResultType == "C");
        Assert.Equal(1, before.NumEligibleToVote);

        SeedPerson(election.ElectionGuid, "Confidential 1", "", canVote: true);
        await _context.SaveChangesAsync();
        await new ElectionAnalyzerNormal(_context, NullLogger.Instance, election).AnalyzeAsync();

        var calc = _context.ResultSummaries.Single(rs =>
            rs.ElectionGuid == election.ElectionGuid && rs.ResultType == "C");
        Assert.Equal(2, calc.NumEligibleToVote);
        Assert.Contains(
            _context.People.Where(p => p.ElectionGuid == election.ElectionGuid),
            p => p.LastName == "Confidential 1" && p.CanVote == true);
    }

    [Fact]
    public async Task ManualEligibleOverride_IsFinalAfterAnalyze()
    {
        var election = SeedElection();
        SeedPerson(election.ElectionGuid, "Ada", "List", canVote: true);
        SeedPerson(election.ElectionGuid, "Confidential 1", "", canVote: true);
        await _context.SaveChangesAsync();

        _context.ResultSummaries.Add(new ResultSummary
        {
            ElectionGuid = election.ElectionGuid,
            ResultType = "M",
            NumEligibleToVote = 1
        });
        await _context.SaveChangesAsync();

        await new ElectionAnalyzerNormal(_context, NullLogger.Instance, election).AnalyzeAsync();

        var calc = _context.ResultSummaries.Single(rs =>
            rs.ElectionGuid == election.ElectionGuid && rs.ResultType == "C");
        var final = _context.ResultSummaries.Single(rs =>
            rs.ElectionGuid == election.ElectionGuid && rs.ResultType == "F");
        Assert.Equal(2, calc.NumEligibleToVote);
        Assert.Equal(1, final.NumEligibleToVote);
    }

    [Fact]
    public async Task SaveManualCounts_PersistsOverride_WithoutReanalyze()
    {
        var election = SeedElection();
        SeedPerson(election.ElectionGuid, "Ada", "List", canVote: true);
        await _context.SaveChangesAsync();

        var service = CreateTallyService();
        var saved = await service.SaveManualCountsAsync(
            election.ElectionGuid,
            new AnalyzeCountRowDto { NumEligibleToVote = 12, InPersonBallots = 3 });

        Assert.Equal(12, saved.Manual.NumEligibleToVote);
        Assert.Equal(3, saved.Manual.InPersonBallots);
        Assert.Equal(12, saved.Final.NumEligibleToVote);
        Assert.Equal(1, saved.Calculated.NumEligibleToVote);

        var stored = _context.ResultSummaries.Single(rs =>
            rs.ElectionGuid == election.ElectionGuid && rs.ResultType == "M");
        Assert.Equal(12, stored.NumEligibleToVote);
        Assert.Equal(3, stored.InPersonBallots);
        Assert.False(_context.ResultSummaries.Any(rs =>
            rs.ElectionGuid == election.ElectionGuid && rs.ResultType == "F"));
    }

    [Fact]
    public async Task CheckInConfidential1_CountsAsInPerson()
    {
        var election = SeedElection();
        var confidential = SeedPerson(election.ElectionGuid, "Confidential 1", "", canVote: true);
        confidential.VotingMethod = VotingMethodCodes.InPerson;
        await _context.SaveChangesAsync();

        await new ElectionAnalyzerNormal(_context, NullLogger.Instance, election).AnalyzeAsync();

        var calc = _context.ResultSummaries.Single(rs =>
            rs.ElectionGuid == election.ElectionGuid && rs.ResultType == "C");
        Assert.Equal(1, calc.InPersonBallots);
        Assert.Equal(1, calc.NumVoters);
    }

    private Election SeedElection()
    {
        var election = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Confidential workflow",
            NumberToElect = 2,
            ElectionType = "LSA",
            ElectionStage = ElectionStage.GatheringBallots,
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

    private Person SeedPerson(Guid electionGuid, string last, string first, bool canVote)
    {
        var person = new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            LastName = last,
            FirstName = first,
            CanVote = canVote,
            CanReceiveVotes = canVote,
            RowVersion = new byte[8]
        };
        _context.People.Add(person);
        return person;
    }

    private TallyService CreateTallyService()
    {
        var localizer = new Mock<IStringLocalizer<TallyService>>();
        localizer.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        return new TallyService(
            _context,
            Mock.Of<ILogger<TallyService>>(),
            Mock.Of<ISignalRNotificationService>(),
            Mock.Of<IComputerAssignmentService>(),
            Mock.Of<IOnlineVoterPresenceService>(),
            localizer.Object);
    }
}
