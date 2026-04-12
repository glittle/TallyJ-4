using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Analyzers;

public class ElectionAnalyzerSingleName : ElectionAnalyzerBase
{
    public ElectionAnalyzerSingleName(MainDbContext context, ILogger logger, Election election)
        : base(context, logger, election)
    {
    }

    protected override bool IsSingleNameElection() => true;

    /// <summary>
    /// Calculates ballot statistics specific to single-name elections, including total votes, spoiled ballots, and ballots needing review.
    /// </summary>
    /// <remarks>
    /// For single-name elections, each ballot can contribute multiple votes if the voter selected multiple candidates.
    /// The ballots status is ignored, since each vote is a ballot on it own.
    /// </remarks>
    protected override void CalculateBallotStatistics()
    {
        Logger.LogInformation("Calculating single-name ballot statistics");

        var totalVotes = Votes.Sum(v => v.SingleNameElectionCount ?? 1);
        ResultSummaryCalc.BallotsReceived = totalVotes;
        ResultSummaryCalc.NumVoters = totalVotes;
        ResultSummaryCalc.TotalVotes = totalVotes;

        var invalidBallotGuids = Ballots
            .Where(b => b.StatusCode != BallotStatus.Ok)
            .Select(b => b.BallotGuid)
            .ToHashSet();

        ResultSummaryCalc.SpoiledBallots = invalidBallotGuids.Count;

        ResultSummaryCalc.SpoiledVotes = Votes
            .Where(v => !invalidBallotGuids.Contains(v.BallotGuid) && v.VoteStatus != VoteStatus.Ok)
            .Sum(v => v.SingleNameElectionCount ?? 1);

        ResultSummaryCalc.BallotsNeedingReview = Votes.Count(v =>
        {
            var person = People.FirstOrDefault(p => p.PersonGuid == v.PersonGuid);
            if (person == null) return false;
            return !string.IsNullOrEmpty(person.CombinedInfo) &&
                   !string.IsNullOrEmpty(v.PersonCombinedInfo) &&
                   !person.CombinedInfo.StartsWith(v.PersonCombinedInfo);
        });
    }

    protected override async Task CountVotesAsync()
    {
        Logger.LogInformation("Starting vote count for single-name election");

        var numProcessed = 0;

        foreach (var vote in Votes.Where(v => v.VoteStatus == VoteStatus.Ok))
        {
            numProcessed++;

            var result = Results.FirstOrDefault(r => r.PersonGuid == vote.PersonGuid);

            if (result == null)
            {
                if (!vote.PersonGuid.HasValue)
                    continue;

                result = new Result
                {
                    ElectionGuid = TargetElection.ElectionGuid,
                    PersonGuid = vote.PersonGuid.Value,
                    VoteCount = 0,
                    Rank = 0,
                    SectionCode = ResultSection.Other,
                    IsTied = false,
                    TieBreakRequired = false,
                    CloseToNext = false,
                    CloseToPrev = false
                };

                Context.Results.Add(result);
                Results.Add(result);
            }

            result.VoteCount = (result.VoteCount ?? 0) + (vote.SingleNameElectionCount ?? 1);
        }

        await Context.SaveChangesAsync();

        Logger.LogInformation("Completed vote count: {VoteCount} vote records processed", numProcessed);
    }
}



