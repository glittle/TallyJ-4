using Backend.Context;
using Backend.DTOs.Results;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests;

/// <summary>
/// v3 Analyze Save Values (ResultType M). Not a confidential-voter step.
/// Panel Final is always manual ?? calculated.
/// </summary>
public class AnalyzeManualCountsTests : IDisposable
{
    private readonly MainDbContext _context;

    public AnalyzeManualCountsTests()
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
    public async Task SaveManualCounts_PersistsOverride_WithoutReanalyze()
    {
        var election = SeedElection();
        SeedPerson(election.ElectionGuid, "Ada", "List");
        await _context.SaveChangesAsync();

        var saved = await CreateTallyService().SaveManualCountsAsync(
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
    public async Task SaveManualCounts_WhenPriorFinalExists_PanelFinalUsesNewManual()
    {
        var election = SeedElection();
        SeedPerson(election.ElectionGuid, "Ada", "List");
        SeedPerson(election.ElectionGuid, "Pat", "List");
        _context.ResultSummaries.AddRange(
            new ResultSummary
            {
                ElectionGuid = election.ElectionGuid,
                ResultType = "C",
                NumEligibleToVote = 2
            },
            new ResultSummary
            {
                ElectionGuid = election.ElectionGuid,
                ResultType = "F",
                NumEligibleToVote = 2
            });
        await _context.SaveChangesAsync();

        var saved = await CreateTallyService().SaveManualCountsAsync(
            election.ElectionGuid,
            new AnalyzeCountRowDto { NumEligibleToVote = 1 });

        Assert.Equal(1, saved.Manual.NumEligibleToVote);
        Assert.Equal(1, saved.Final.NumEligibleToVote);
        Assert.Equal(2, saved.Calculated.NumEligibleToVote);

        var storedFinal = _context.ResultSummaries.Single(rs =>
            rs.ElectionGuid == election.ElectionGuid && rs.ResultType == "F");
        Assert.Equal(2, storedFinal.NumEligibleToVote);
    }

    private Election SeedElection()
    {
        var election = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Analyze Save Values",
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

    private Person SeedPerson(Guid electionGuid, string last, string first)
    {
        var person = new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            LastName = last,
            FirstName = first,
            CanVote = true,
            CanReceiveVotes = true,
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
