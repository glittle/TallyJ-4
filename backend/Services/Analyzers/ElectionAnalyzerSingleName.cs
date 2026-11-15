using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
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
    /// Single-name totals. Each vote row is its own ballot, and
    /// <c>SingleNameElectionCount</c> is how many ballots that name got.
    /// <c>BallotsReceived</c>, <c>NumVoters</c>, and <c>TotalVotes</c> are that
    /// sum, not a count of ballot rows. <c>SpoiledBallots</c> is still the
    /// ballot rows whose status is not Ok. A not-Ok vote adds its name-count
    /// to <c>SpoiledVotes</c> only when its ballot row is Ok.
    /// </summary>
    protected override void CalculateBallotStatistics()
    {
        Logger.LogInformation("Calculating single-name ballot statistics");

        var totalVotes = Votes.Sum(v => v.SingleNameElectionCount ?? 1);
        ResultSummaryCalc.BallotsReceived = totalVotes;
        ResultSummaryCalc.NumVoters = totalVotes;
        ResultSummaryCalc.TotalVotes = totalVotes;

        var notOkBallotGuids = Ballots
            .Where(b => b.StatusCode != BallotStatus.Ok)
            .Select(b => b.BallotGuid)
            .ToHashSet();

        ResultSummaryCalc.SpoiledBallots = notOkBallotGuids.Count;

        ResultSummaryCalc.SpoiledVotes = Votes
            .Where(v => !notOkBallotGuids.Contains(v.BallotGuid) && v.VoteStatus != VoteStatus.Ok)
            .Sum(v => v.SingleNameElectionCount ?? 1);

        // A vote whose stored name no longer matches the person. Not the
        // ballot-status review set used by a normal election.
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
                    Section = OtherSection,
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



