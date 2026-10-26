using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using static Backend.Enumerations.ElectionStageMessageKeys;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

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
    public async Task EvaluateAsync_WorksOnSqliteWithoutThrowing()
    {
        var electionGuid = Guid.NewGuid();
        var dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        await using (var context = new MainDbContext(options))
        {
            context.Database.EnsureCreated();
            var bestId = Guid.NewGuid();
            var coleId = Guid.NewGuid();
            context.Elections.Add(new Election
            {
                ElectionGuid = electionGuid,
                Name = "Sqlite Election",
                ElectionType = "LSA",
                NumberToElect = 3,
                NumberExtra = 1,
                ElectionStage = ElectionStage.ProcessingBallots,
                DateOfElection = DateTime.UtcNow,
                RowVersion = new byte[8]
            });
            context.People.AddRange(
                new Person
                {
                    PersonGuid = bestId,
                    ElectionGuid = electionGuid,
                    FirstName = "Pat",
                    LastName = "Best",
                    RowVersion = new byte[8]
                },
                new Person
                {
                    PersonGuid = coleId,
                    ElectionGuid = electionGuid,
                    FirstName = "Sam",
                    LastName = "Cole",
                    RowVersion = new byte[8]
                });
            // Calculate writes both summaries, then an unresolved tie leaves UseOnReports false.
            context.ResultSummaries.Add(CalculatedSummary(electionGuid, "C"));
            context.ResultSummaries.Add(CalculatedSummary(electionGuid, "F"));
            context.Results.AddRange(
                new Result
                {
                    ElectionGuid = electionGuid,
                    PersonGuid = bestId,
                    Rank = 1,
                    Section = "E",
                    VoteCount = 10,
                    IsTied = true,
                    TieBreakGroup = 1,
                    TieBreakRequired = true,
                    TieBreakCount = null,
                    IsTieResolved = false,
                    CloseToPrev = false,
                    CloseToNext = true
                },
                new Result
                {
                    ElectionGuid = electionGuid,
                    PersonGuid = coleId,
                    Rank = 2,
                    Section = "X",
                    VoteCount = 10,
                    IsTied = true,
                    TieBreakGroup = 1,
                    TieBreakRequired = true,
                    TieBreakCount = null,
                    IsTieResolved = false,
                    CloseToPrev = true,
                    CloseToNext = false,
                    RankInExtra = 1
                });
            context.ResultTies.Add(new ResultTie
            {
                ElectionGuid = electionGuid,
                TieBreakGroup = 1,
                TieBreakRequired = true,
                IsResolved = false,
                NumInTie = 2,
                NumToElect = 1
            });
            await context.SaveChangesAsync();

            var readiness = await ElectionStageFinalizationReadiness.EvaluateAsync(context, electionGuid);

            Assert.False(readiness.IsReady);
            Assert.Contains(readiness.Blockers, blocker => blocker == AnalysisNotReady);
            Assert.Contains(readiness.Blockers, blocker => blocker == UnresolvedTies);
        }

        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var path = dbPath + suffix;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
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
        Assert.Contains(readiness.Blockers, b => b == AnalysisNotCompleted);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsCountsDoNotReconcileWhenFrontDeskDoesNotMatchBallots()
    {
        var electionGuid = Guid.NewGuid();
        var personGuid = Guid.NewGuid();
        var locationGuid = Guid.NewGuid();

        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Unreconciled Election",
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
            VotingMethod = "P",
            RowVersion = new byte[8]
        });
        Context.Locations.Add(new Location
        {
            LocationGuid = locationGuid,
            ElectionGuid = electionGuid,
            Name = "Hall"
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

        Assert.False(readiness.IsReady);
        Assert.Contains(readiness.Blockers, b => b.StartsWith(CountsDoNotReconcile));
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsOnlineVotingStillOpenWhenWindowIsOpen()
    {
        var electionGuid = await SeedReadyElectionAsync(useOnlineVoting: true);
        var election = Context.Elections.Single(e => e.ElectionGuid == electionGuid);
        election.OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-1);
        election.OnlineWhenClose = DateTimeOffset.UtcNow.AddHours(1);
        await Context.SaveChangesAsync();

        var readiness = await ElectionStageFinalizationReadiness.EvaluateAsync(Context, electionGuid);

        Assert.False(readiness.IsReady);
        Assert.Contains(OnlineVotingStillOpen, readiness.Blockers);
    }

    [Fact]
    public async Task EvaluateAsync_ReadyWhenOnlineVotingEnabledButWindowClosed()
    {
        var electionGuid = await SeedReadyElectionAsync(useOnlineVoting: true);
        var election = Context.Elections.Single(e => e.ElectionGuid == electionGuid);
        election.OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-2);
        election.OnlineWhenClose = DateTimeOffset.UtcNow.AddHours(-1);
        await Context.SaveChangesAsync();

        var readiness = await ElectionStageFinalizationReadiness.EvaluateAsync(Context, electionGuid);

        Assert.True(readiness.IsReady);
        Assert.Empty(readiness.Blockers);
    }

    [Fact]
    public async Task EvaluateAsync_ResolvedTie_DoesNotBlockFinalization()
    {
        var electionGuid = await SeedReadyElectionAsync(useOnlineVoting: false);
        var result = Context.Results.Single(r => r.ElectionGuid == electionGuid);
        result.IsTied = true;
        result.TieBreakRequired = true;
        result.IsTieResolved = true;
        result.TieBreakCount = 3;
        Context.ResultTies.Add(new ResultTie
        {
            ElectionGuid = electionGuid,
            TieBreakGroup = 1,
            TieBreakRequired = true,
            IsResolved = true,
            NumInTie = 2,
            NumToElect = 1
        });
        await Context.SaveChangesAsync();

        var readiness = await ElectionStageFinalizationReadiness.EvaluateAsync(Context, electionGuid);

        Assert.True(readiness.IsReady);
        Assert.DoesNotContain(UnresolvedTies, readiness.Blockers);
    }

    [Fact]
    public async Task EvaluateAsync_UnresolvedResultTie_BlocksFinalization()
    {
        var electionGuid = await SeedReadyElectionAsync(useOnlineVoting: false);
        var result = Context.Results.Single(r => r.ElectionGuid == electionGuid);
        result.IsTied = true;
        result.TieBreakRequired = true;
        result.IsTieResolved = false;
        Context.ResultTies.Add(new ResultTie
        {
            ElectionGuid = electionGuid,
            TieBreakGroup = 1,
            TieBreakRequired = true,
            IsResolved = true,
            NumInTie = 2,
            NumToElect = 1
        });
        await Context.SaveChangesAsync();

        var readiness = await ElectionStageFinalizationReadiness.EvaluateAsync(Context, electionGuid);

        Assert.False(readiness.IsReady);
        Assert.Contains(UnresolvedTies, readiness.Blockers);
    }

    [Fact]
    public async Task EvaluateAsync_ReadyWhenUseOnlineVotingIsFalse()
    {
        var electionGuid = await SeedReadyElectionAsync(useOnlineVoting: false);

        var readiness = await ElectionStageFinalizationReadiness.EvaluateAsync(Context, electionGuid);

        Assert.True(readiness.IsReady);
        Assert.Empty(readiness.Blockers);
    }

    private async Task<Guid> SeedReadyElectionAsync(bool useOnlineVoting)
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
            UseOnlineVoting = useOnlineVoting,
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
        return electionGuid;
    }

    /// <summary>
    /// Count fields <c>CombineCalcAndManualSummaries</c> writes after Calculate, with an unresolved tie.
    /// </summary>
    private static ResultSummary CalculatedSummary(Guid electionGuid, string resultType)
    {
        return new ResultSummary
        {
            ElectionGuid = electionGuid,
            ResultType = resultType,
            UseOnReports = false,
            NumVoters = 0,
            NumEligibleToVote = 0,
            MailedInBallots = 0,
            DroppedOffBallots = 0,
            InPersonBallots = 0,
            SpoiledBallots = 0,
            BallotsReceived = 0,
            BallotsNeedingReview = 0,
            CalledInBallots = 0,
            OnlineBallots = 0,
            Custom1Ballots = 0,
            Custom2Ballots = 0,
            Custom3Ballots = 0,
            ImportedBallots = 0
        };
    }
}