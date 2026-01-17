using Microsoft.EntityFrameworkCore;
using TallyJ4.EF.Context;
using TallyJ4.Domain.Entities;

namespace TallyJ4.Services.Analyzers;

public abstract class ElectionAnalyzerBase
{
    protected readonly MainDbContext Context;
    protected readonly ILogger Logger;
    protected readonly Election TargetElection;

    protected List<Ballot> Ballots = new();
    protected List<Vote> Votes = new();
    protected List<Person> People = new();
    protected List<Result> Results = new();
    protected ResultSummary ResultSummaryCalc = new();

    protected ElectionAnalyzerBase(MainDbContext context, ILogger logger, Election election)
    {
        Context = context;
        Logger = logger;
        TargetElection = election;
    }

    public async Task AnalyzeAsync()
    {
        await PrepareForAnalysisAsync();
        CalculateBallotStatistics();
        await CountVotesAsync();
        FinalizeResultsAndTies();
        await FinalizeSummariesAsync();
        await SaveResultsAsync();
    }

    protected virtual async Task PrepareForAnalysisAsync()
    {
        Logger.LogInformation("Preparing for analysis of election {ElectionGuid}", TargetElection.ElectionGuid);

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

        Results = await Context.Results
            .Where(r => r.ElectionGuid == TargetElection.ElectionGuid)
            .ToListAsync();

        ResultSummaryCalc = await Context.ResultSummaries
            .FirstOrDefaultAsync(rs => rs.ElectionGuid == TargetElection.ElectionGuid)
            ?? new ResultSummary { ElectionGuid = TargetElection.ElectionGuid, ResultType = "N" };

        Logger.LogInformation("Loaded {BallotCount} ballots, {VoteCount} votes, {PeopleCount} people",
            Ballots.Count, Votes.Count, People.Count);
    }

    protected virtual void CalculateBallotStatistics()
    {
        Logger.LogInformation("Calculating ballot statistics");

        var invalidBallotGuids = Ballots
            .Where(b => b.StatusCode != "Ok")
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
                       DetermineVoteStatus(v) != "Ok")
            .ToList();

        ResultSummaryCalc.SpoiledVotes = invalidVotes.Count;

        Logger.LogInformation("Statistics: {TotalBallots} ballots ({SpoiledBallots} spoiled), {TotalVotes} potential votes ({SpoiledVotes} spoiled)",
            totalBallots, spoiledBallots, ResultSummaryCalc.TotalVotes, ResultSummaryCalc.SpoiledVotes);
    }

    protected abstract Task CountVotesAsync();

    protected virtual void FinalizeResultsAndTies()
    {
        Logger.LogInformation("Finalizing results and detecting ties");

        var groupedByVotes = Results
            .GroupBy(r => r.VoteCount ?? 0)
            .OrderByDescending(g => g.Key)
            .ToList();

        var rank = 1;
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

                if (rank <= numberToElect)
                {
                    result.Section = "E";
                }
                else if (rank <= numberToElect + numberExtra)
                {
                    result.Section = "X";
                }
                else
                {
                    result.Section = "O";
                }
            }

            rank += candidatesInGroup.Count;
            if (isTied) tieGroupNumber++;
        }

        foreach (var group in groupedByVotes.Where(g => g.Count() > 1))
        {
            var sections = group.Select(r => r.Section).Distinct().ToList();

            if (sections.Count > 1)
            {
                foreach (var result in group)
                {
                    result.TieBreakRequired = true;
                }
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

    protected virtual async Task SaveResultsAsync()
    {
        Logger.LogInformation("Saving results to database");
        await Context.SaveChangesAsync();
        Logger.LogInformation("Results saved successfully");
    }

    protected virtual bool BallotNeedsReview(Ballot ballot)
    {
        if (ballot.StatusCode != "Ok")
            return true;

        var ballotVotes = Votes.Where(v => v.BallotGuid == ballot.BallotGuid).ToList();
        var numberToElect = TargetElection.NumberToElect ?? 9;

        if (ballotVotes.Count != numberToElect)
            return true;

        if (ballotVotes.Any(v => DetermineVoteStatus(v) == "Changed"))
            return true;

        return false;
    }

    protected virtual string DetermineVoteStatus(Vote vote)
    {
        var person = People.FirstOrDefault(p => p.PersonGuid == vote.PersonGuid);

        if (person == null)
            return "Spoiled";

        if (person.CanReceiveVotes != true)
            return "Spoiled";

        if (!string.IsNullOrEmpty(vote.PersonCombinedInfo) &&
            !string.IsNullOrEmpty(person.CombinedInfo) &&
            !person.CombinedInfo.StartsWith(vote.PersonCombinedInfo))
        {
            return "Changed";
        }

        return "Ok";
    }
}
