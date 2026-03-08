using Microsoft.EntityFrameworkCore;
using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;

namespace Backend.Services.Analyzers;

/// <summary>
/// Base class for election result analyzers.
/// Provides common functionality for calculating election tallies, processing votes, and determining winners.
/// </summary>
public abstract class ElectionAnalyzerBase
{
    /// <summary>
    /// The database context for accessing election data.
    /// </summary>
    protected readonly MainDbContext Context;

    /// <summary>
    /// The logger for recording analysis operations.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// The election being analyzed.
    /// </summary>
    protected readonly Election TargetElection;

    /// <summary>
    /// Collection of ballots for the election.
    /// </summary>
    protected List<Ballot> Ballots = new();

    /// <summary>
    /// Collection of votes from all ballots.
    /// </summary>
    protected List<Vote> Votes = new();

    /// <summary>
    /// Collection of people (candidates) in the election.
    /// </summary>
    protected List<Person> People = new();

    /// <summary>
    /// Collection of calculated results.
    /// </summary>
    protected List<Result> Results = new();

    /// <summary>
    /// Collection of tie records.
    /// </summary>
    protected List<ResultTie> ResultTies = new();

    /// <summary>
    /// The result summary being calculated.
    /// </summary>
    protected ResultSummary ResultSummaryCalc = new();

    /// <summary>
    /// Initializes a new instance of the ElectionAnalyzerBase.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="election">The election to analyze.</param>
    protected ElectionAnalyzerBase(MainDbContext context, ILogger logger, Election election)
    {
        Context = context;
        Logger = logger;
        TargetElection = election;
    }

    /// <summary>
    /// Performs the complete election analysis process.
    /// Includes data preparation, vote counting, result calculation, and persistence.
    /// </summary>
    /// <returns>A task representing the asynchronous analysis operation.</returns>
    public async Task AnalyzeAsync()
    {
        using var transaction = await Context.Database.BeginTransactionAsync();
        try
        {
            Logger.LogInformation("Starting tally calculation transaction for election {ElectionGuid}", TargetElection.ElectionGuid);

            await PrepareForAnalysisAsync();
            CalculateBallotStatistics();
            await CountVotesAsync();
            FinalizeResultsAndTies();
            await FinalizeSummariesAsync();
            await SaveResultsAsync();

            await transaction.CommitAsync();
            Logger.LogInformation("Tally calculation transaction committed successfully for election {ElectionGuid}", TargetElection.ElectionGuid);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during tally calculation for election {ElectionGuid}. Transaction will be rolled back.", TargetElection.ElectionGuid);
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Prepares the analyzer for analysis by loading required data and clearing previous results.
    /// </summary>
    /// <returns>A task representing the asynchronous preparation operation.</returns>
    protected virtual async Task PrepareForAnalysisAsync()
    {
        Logger.LogInformation("Preparing for analysis of election {ElectionGuid}", TargetElection.ElectionGuid);

        var existingResults = await Context.Results
            .Where(r => r.ElectionGuid == TargetElection.ElectionGuid)
            .ToListAsync();

        if (existingResults.Any())
        {
            Context.Results.RemoveRange(existingResults);
            Logger.LogInformation("Cleared {ResultCount} existing Result records", existingResults.Count);
        }

        var existingResultTies = await Context.ResultTies
            .Where(rt => rt.ElectionGuid == TargetElection.ElectionGuid)
            .ToListAsync();

        if (existingResultTies.Any())
        {
            Context.ResultTies.RemoveRange(existingResultTies);
            Logger.LogInformation("Cleared {ResultTieCount} existing ResultTie records", existingResultTies.Count);
        }

        await Context.SaveChangesAsync();

        var locationGuids = await Context.Locations
            .Where(l => l.ElectionGuid == TargetElection.ElectionGuid)
            .Select(l => l.LocationGuid)
            .ToListAsync();

        Ballots = await Context.Ballots
            .Where(b => locationGuids.Contains(b.LocationGuid))
            .ToListAsync();

        var ballotGuids = Ballots.Select(b => b.BallotGuid).ToList();

        Votes = await Context.Votes
            .Where(v => ballotGuids.Contains(v.BallotGuid))
            .ToListAsync();

        People = await Context.People
            .Where(p => p.ElectionGuid == TargetElection.ElectionGuid)
            .ToListAsync();

        Results = new List<Result>();

        ResultSummaryCalc = await Context.ResultSummaries
            .FirstOrDefaultAsync(rs => rs.ElectionGuid == TargetElection.ElectionGuid)
            ?? new ResultSummary { ElectionGuid = TargetElection.ElectionGuid, ResultType = "N" };

        Logger.LogInformation("Loaded {BallotCount} ballots, {VoteCount} votes, {PeopleCount} people",
            Ballots.Count, Votes.Count, People.Count);
    }

    /// <summary>
    /// Calculates statistics about ballots and votes for the election.
    /// Determines counts of valid, spoiled, and review-needed ballots.
    /// </summary>
    protected virtual void CalculateBallotStatistics()
    {
        Logger.LogInformation("Calculating ballot statistics");

        var invalidBallotGuids = Ballots
            .Where(b => b.StatusCode != BallotStatus.Ok)
            .Select(b => b.BallotGuid)
            .ToList();

        var totalBallots = Ballots.Count;
        var spoiledBallots = invalidBallotGuids.Count;

        ResultSummaryCalc.BallotsReceived = totalBallots - spoiledBallots;
        ResultSummaryCalc.SpoiledBallots = spoiledBallots;
        ResultSummaryCalc.BallotsNeedingReview = Ballots.Count(b => BallotNeedsReview(b));

        var numberToElect = TargetElection.NumberToElect ?? 9;
        ResultSummaryCalc.TotalVotes = totalBallots * numberToElect;

        var invalidVotes = Votes
            .Where(v => !invalidBallotGuids.Contains(v.BallotGuid) &&
                       DetermineVoteStatus(v) != VoteStatus.Ok)
            .ToList();

        ResultSummaryCalc.SpoiledVotes = invalidVotes.Count;

        Logger.LogInformation("Statistics: {TotalBallots} ballots ({SpoiledBallots} spoiled), {TotalVotes} potential votes ({SpoiledVotes} spoiled)",
            totalBallots, spoiledBallots, ResultSummaryCalc.TotalVotes, ResultSummaryCalc.SpoiledVotes);
    }

    /// <summary>
    /// Counts the votes for the election according to the specific election type rules.
    /// </summary>
    /// <returns>A task representing the asynchronous vote counting operation.</returns>
    protected abstract Task CountVotesAsync();

    /// <summary>
    /// Finalizes the election results by assigning ranks and detecting ties.
    /// </summary>
    protected virtual void FinalizeResultsAndTies()
    {
        Logger.LogInformation("Finalizing results and detecting ties");

        var groupedByVotes = Results
            .GroupBy(r => r.VoteCount ?? 0)
            .OrderByDescending(g => g.Key)
            .ToList();

        var rank = 1;
        var ordinal = 1;
        var tieGroupNumber = 1;
        var numberToElect = TargetElection.NumberToElect ?? 9;
        var numberExtra = TargetElection.NumberExtra ?? 0;

        foreach (var group in groupedByVotes)
        {
            var candidatesInGroup = group.ToList();
            var isTied = candidatesInGroup.Count > 1;

            foreach (var result in candidatesInGroup)
            {
                result.Rank = rank;
                result.IsTied = isTied;

                if (isTied)
                {
                    result.TieBreakGroup = tieGroupNumber;
                }

                if (ordinal <= numberToElect)
                {
                    result.Section = "E";
                }
                else if (ordinal <= numberToElect + numberExtra)
                {
                    result.Section = "X";
                }
                else
                {
                    result.Section = "O";
                }

                ordinal++;
            }

            rank += candidatesInGroup.Count;
            if (isTied) tieGroupNumber++;
        }

        foreach (var group in groupedByVotes.Where(g => g.Count() > 1))
        {
            var sections = group.Select(r => r.Section).Distinct().ToList();

            if (sections.Count > 1)
            {
                var tieBreakGroup = 0;
                foreach (var result in group)
                {
                    result.TieBreakRequired = true;
                    tieBreakGroup = result.TieBreakGroup ?? 0;
                }

                var resultTie = new ResultTie
                {
                    ElectionGuid = TargetElection.ElectionGuid,
                    TieBreakGroup = tieBreakGroup,
                    TieBreakRequired = true,
                    NumInTie = group.Count(),
                    NumToElect = numberToElect
                };
                ResultTies.Add(resultTie);
            }
        }

        var resultsOrdered = Results.OrderBy(r => r.Rank).ToList();
        for (int i = 0; i < resultsOrdered.Count; i++)
        {
            var current = resultsOrdered[i];

            if (i > 0)
            {
                var prev = resultsOrdered[i - 1];
                var diff = (prev.VoteCount ?? 0) - (current.VoteCount ?? 0);
                current.CloseToPrev = diff >= 1 && diff <= 3;
            }

            if (i < resultsOrdered.Count - 1)
            {
                var next = resultsOrdered[i + 1];
                var diff = (current.VoteCount ?? 0) - (next.VoteCount ?? 0);
                current.CloseToNext = diff >= 1 && diff <= 3;
            }
        }

        Logger.LogInformation("Finalized {ResultCount} results, {TieCount} tie groups",
            Results.Count, groupedByVotes.Count(g => g.Count() > 1));
    }

    /// <summary>
    /// Finalizes the result summary and saves it to the database.
    /// </summary>
    /// <returns>A task representing the asynchronous summary finalization operation.</returns>
    protected virtual async Task FinalizeSummariesAsync()
    {
        Logger.LogInformation("Finalizing summaries");

        ResultSummaryCalc.UseOnReports = true;
        ResultSummaryCalc.ElectionGuid = TargetElection.ElectionGuid;

        var existingSummary = await Context.ResultSummaries
            .FirstOrDefaultAsync(rs => rs.ElectionGuid == TargetElection.ElectionGuid);

        if (existingSummary == null)
        {
            Context.ResultSummaries.Add(ResultSummaryCalc);
        }
        else
        {
            existingSummary.BallotsReceived = ResultSummaryCalc.BallotsReceived;
            existingSummary.SpoiledBallots = ResultSummaryCalc.SpoiledBallots;
            existingSummary.BallotsNeedingReview = ResultSummaryCalc.BallotsNeedingReview;
            existingSummary.TotalVotes = ResultSummaryCalc.TotalVotes;
            existingSummary.SpoiledVotes = ResultSummaryCalc.SpoiledVotes;
            existingSummary.UseOnReports = true;
        }
    }

    /// <summary>
    /// Saves the calculated results and tie records to the database.
    /// </summary>
    /// <returns>A task representing the asynchronous save operation.</returns>
    protected virtual async Task SaveResultsAsync()
    {
        Logger.LogInformation("Saving results to database");

        if (ResultTies.Any())
        {
            Context.ResultTies.AddRange(ResultTies);
            Logger.LogInformation("Added {ResultTieCount} ResultTie records", ResultTies.Count);
        }

        await Context.SaveChangesAsync();
        Logger.LogInformation("Results saved successfully");
    }

    /// <summary>
    /// Determines if a ballot needs review based on its status and vote validity.
    /// </summary>
    /// <param name="ballot">The ballot to check.</param>
    /// <returns>True if the ballot needs review, false otherwise.</returns>
    protected virtual bool BallotNeedsReview(Ballot ballot)
    {
        if (ballot.StatusCode != BallotStatus.Ok)
            return true;

        var ballotVotes = Votes.Where(v => v.BallotGuid == ballot.BallotGuid).ToList();
        var numberToElect = TargetElection.NumberToElect ?? 9;

        if (ballotVotes.Count != numberToElect)
            return true;

        if (ballotVotes.Any(v => DetermineVoteStatus(v) == VoteStatus.Changed))
            return true;

        return false;
    }

    /// <summary>
    /// Determines the status of a vote based on candidate eligibility and data consistency.
    /// </summary>
    /// <param name="vote">The vote to evaluate.</param>
    /// <returns>The vote status.</returns>
    protected virtual VoteStatus DetermineVoteStatus(Vote vote)
    {
        var person = People.FirstOrDefault(p => p.PersonGuid == vote.PersonGuid);

        if (person == null)
            return VoteStatus.Spoiled;

        if (person.CanReceiveVotes != true)
            return VoteStatus.Spoiled;

        if (!string.IsNullOrEmpty(vote.PersonCombinedInfo) &&
            !string.IsNullOrEmpty(person.CombinedInfo) &&
            !person.CombinedInfo.StartsWith(vote.PersonCombinedInfo))
        {
            return VoteStatus.Changed;
        }

        return VoteStatus.Ok;
    }
}



