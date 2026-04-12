using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Analyzers;

public class ElectionAnalyzerNormal : ElectionAnalyzerBase
{
    public ElectionAnalyzerNormal(MainDbContext context, ILogger logger, Election election)
        : base(context, logger, election)
    {
    }

    protected override async Task CountVotesAsync()
    {
        Logger.LogInformation("Starting vote count for normal election");

        var numProcessed = 0;
        var numVotesTotal = 0;

        foreach (var ballot in Ballots.Where(b => b.StatusCode == BallotStatus.Ok))
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
                if (vote.VoteStatus != VoteStatus.Ok)
                    continue;

                numVotesTotal++;

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

                result.VoteCount = (result.VoteCount ?? 0) + 1;
            }
        }

        await Context.SaveChangesAsync();

        Logger.LogInformation("Completed vote count: {BallotCount} ballots processed, {VoteCount} valid votes",
            numProcessed, numVotesTotal);
    }
}



