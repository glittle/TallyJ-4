using Backend.Entities;
using Backend.Enumerations;
using Backend.Services;

namespace Backend.Tests.UnitTests;

public class ElectionStageFinalizationReadinessTests : ServiceTestBase
{
    [Fact]
    public async Task EvaluateAsync_ReturnsReadyWhenPrerequisitesMet()
    {
        var electionGuid = Guid.NewGuid();
        var personGuid = Guid.NewGuid();

        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Ready Election",
            ElectionType = "LSA",
            NumberToElect = 3,
            ElectionStage = ElectionStage.ProcessingBallots,
            DateOfElection = DateTime.UtcNow,
            RowVersion = new byte[8]
        });
        Context.People.Add(new Person
        {
            PersonGuid = personGuid,
            ElectionGuid = electionGuid,
            FirstName = "A",
            LastName = "B",
            RowVersion = new byte[8]
        });
        Context.Results.Add(new Result
        {
            ElectionGuid = electionGuid,
            PersonGuid = personGuid,
            Rank = 1,
            Section = "E",
            VoteCount = 5
        });
        Context.ResultSummaries.Add(new ResultSummary
        {
            ElectionGuid = electionGuid,
            ResultType = "F",
            UseOnReports = true,
            BallotsNeedingReview = 0
        });
        await Context.SaveChangesAsync();

        var readiness = await ElectionStageFinalizationReadiness.EvaluateAsync(Context, electionGuid);

        Assert.True(readiness.IsReady);
        Assert.Empty(readiness.Blockers);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsBlockersWhenAnalysisMissing()
    {
        var electionGuid = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Unanalyzed Election",
            ElectionType = "LSA",
            NumberToElect = 3,
            ElectionStage = ElectionStage.ProcessingBallots,
            DateOfElection = DateTime.UtcNow,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();

        var readiness = await ElectionStageFinalizationReadiness.EvaluateAsync(Context, electionGuid);

        Assert.False(readiness.IsReady);
        Assert.Contains(readiness.Blockers, b => b.Contains("analysis", StringComparison.OrdinalIgnoreCase));
    }
}