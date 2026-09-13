using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services.Analyzers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.UnitTests;

/// <summary>
/// #186 confidential voter: add as “Confidential X” (ordinary person).
/// Calculated eligible and in-person increase. There is no override step.
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
            Name = "Confidential 1 is an ordinary person",
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
}
