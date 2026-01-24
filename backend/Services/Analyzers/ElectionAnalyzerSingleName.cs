using Microsoft.EntityFrameworkCore;
using TallyJ4.Domain.Context;
using TallyJ4.Domain.Entities;

namespace TallyJ4.Services.Analyzers;

public class ElectionAnalyzerSingleName : ElectionAnalyzerBase
{
    public ElectionAnalyzerSingleName(MainDbContext context, ILogger logger, Election election)
        : base(context, logger, election)
    {
    }

    protected override async Task CountVotesAsync()
    {
        Logger.LogInformation("Starting vote count for single-name election");

        var validBallotGuids = Ballots
            .Where(b => b.StatusCode == "Ok")
            .Select(b => b.BallotGuid)
            .ToHashSet();

        var numProcessed = 0;
        var numVotesTotal = 0;

        foreach (var ballot in Ballots.Where(b => b.StatusCode == "Ok"))
        {
            numProcessed++;

            if (numProcessed % 10 == 0)
            {
                Logger.LogInformation("Processed {BallotCount} ballots ({VoteCount} votes)",
                    numProcessed, numVotesTotal);
            }

            var ballotVotes = Votes.Where(v => v.BallotGuid == ballot.BallotGuid).ToList();

            foreach (var vote in ballotVotes)
            {
                var voteStatus = DetermineVoteStatus(vote);

                if (voteStatus != "Ok")
                    continue;

                var voteCount = vote.SingleNameElectionCount ?? 1;
                numVotesTotal += voteCount;

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
                        Section = "O",
                        IsTied = false,
                        TieBreakRequired = false,
                        CloseToNext = false,
                        CloseToPrev = false
                    };

                    Context.Results.Add(result);
                    Results.Add(result);
                }

                result.VoteCount = (result.VoteCount ?? 0) + voteCount;
            }
        }

        await Context.SaveChangesAsync();

        Logger.LogInformation("Completed vote count: {BallotCount} ballots processed, {VoteCount} valid votes",
            numProcessed, numVotesTotal);
    }
}
