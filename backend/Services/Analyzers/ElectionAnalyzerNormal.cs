using Microsoft.EntityFrameworkCore;
using Backend.Domain.Context;
using Backend.Domain.Entities;

namespace Backend.Services.Analyzers;

/// <summary>
/// Election analyzer for normal elections where each vote counts as one.
/// Implements standard vote counting logic for typical elections.
/// </summary>
public class ElectionAnalyzerNormal : ElectionAnalyzerBase
{
    /// <summary>
    /// Initializes a new instance of the ElectionAnalyzerNormal.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="election">The election to analyze.</param>
    public ElectionAnalyzerNormal(MainDbContext context, ILogger logger, Election election)
        : base(context, logger, election)
    {
    }

    /// <summary>
    /// Counts votes for a normal election where each valid vote counts as one.
    /// Processes ballots and aggregates vote counts for each candidate.
    /// </summary>
    /// <returns>A task representing the asynchronous vote counting operation.</returns>
    protected override async Task CountVotesAsync()
    {
        Logger.LogInformation("Starting vote count for normal election");

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
                        Section = "O",
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



