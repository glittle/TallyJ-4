using Microsoft.EntityFrameworkCore;
using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;

namespace Backend.Services.Analyzers;

public abstract class ElectionAnalyzerBase
{
    private const int ThresholdForCloseVote = 3;

    protected readonly MainDbContext Context;
    protected readonly ILogger Logger;
    protected readonly Election TargetElection;

    protected List<Ballot> Ballots = new();
    protected List<Vote> Votes = new();
    protected List<Person> People = new();
    protected List<Result> Results = new();
    protected List<ResultTie> ResultTies = new();
    protected ResultSummary ResultSummaryCalc = new();
    protected ResultSummary ResultSummaryFinal = new();
    private Dictionary<Guid, int> _previousTieBreakCounts = new();

    protected ElectionAnalyzerBase(MainDbContext context, ILogger logger, Election election)
    {
        Context = context;
        Logger = logger;
        TargetElection = election;
    }

    public async Task AnalyzeAsync()
    {
        if (TargetElection.NumberToElect == null)
        {
            throw new InvalidOperationException($"Election {TargetElection.ElectionGuid} has no NumberToElect specified. This is required for analysis.");
        }

        using var transaction = await Context.Database.BeginTransactionAsync();
        try
        {
            Logger.LogInformation("Starting tally calculation transaction for election {ElectionGuid}", TargetElection.ElectionGuid);

            await PrepareForAnalysisAsync();
            RefreshBallotStatuses();
            FillResultSummaryCalc();
            CalculateBallotStatistics();
            await CountVotesAsync();
            FinalizeResultsAndTies();
            FinalizeSummaries();
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

    protected virtual async Task PrepareForAnalysisAsync()
    {
        Logger.LogInformation("Preparing for analysis of election {ElectionGuid}", TargetElection.ElectionGuid);

        var existingResults = await Context.Results
            .Where(r => r.ElectionGuid == TargetElection.ElectionGuid)
            .ToListAsync();

        _previousTieBreakCounts = existingResults
            .Where(r => r.TieBreakCount.HasValue)
            .ToDictionary(r => r.PersonGuid, r => r.TieBreakCount!.Value);

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

        var existingNonManualSummaries = await Context.ResultSummaries
            .Where(rs => rs.ElectionGuid == TargetElection.ElectionGuid && rs.ResultType != "M")
            .ToListAsync();

        if (existingNonManualSummaries.Any())
        {
            Context.ResultSummaries.RemoveRange(existingNonManualSummaries);
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
        ResultTies = new List<ResultTie>();

        ResultSummaryCalc = new ResultSummary
        {
            ElectionGuid = TargetElection.ElectionGuid,
            ResultType = "C"
        };
        Context.ResultSummaries.Add(ResultSummaryCalc);

        ResultSummaryFinal = new ResultSummary
        {
            ElectionGuid = TargetElection.ElectionGuid,
            ResultType = "F"
        };
        Context.ResultSummaries.Add(ResultSummaryFinal);

        Logger.LogInformation("Loaded {BallotCount} ballots, {VoteCount} votes, {PeopleCount} people",
            Ballots.Count, Votes.Count, People.Count);
    }

    protected virtual void RefreshBallotStatuses()
    {
        foreach (var vote in Votes)
        {
            var newStatus = DetermineVoteStatus(vote);
            vote.VoteStatus = newStatus;
        }

        var isSingleName = IsSingleNameElection();
        var numberToElect = TargetElection.NumberToElect.Value;
        var ballotAnalyzer = new BallotAnalyzer(numberToElect, isSingleName);

        // Pre-group votes by BallotGuid for O(1) lookup instead of O(votes) per ballot
        var votesByBallotGuid = Votes.ToLookup(v => v.BallotGuid);

        foreach (var ballot in Ballots)
        {
            var ballotVotes = votesByBallotGuid[ballot.BallotGuid].ToList();
            var voteInfos = ballotVotes.Select(v => CreateBallotVoteInfo(v)).ToList();
            ballotAnalyzer.DetermineStatusFromVotes(ballot.StatusCode, voteInfos, out var newStatus, out _);
            ballot.StatusCode = newStatus;
        }
    }

    protected BallotVoteInfo CreateBallotVoteInfo(Vote vote)
    {
        var person = People.FirstOrDefault(p => p.PersonGuid == vote.PersonGuid);
        return new BallotVoteInfo
        {
            PersonGuid = vote.PersonGuid,
            PersonCanReceiveVotes = person?.CanReceiveVotes ?? false,
            PersonCombinedInfo = person?.CombinedInfo,
            VoteCombinedInfo = vote.PersonCombinedInfo,
            VoteIneligibleReasonCode = vote.IneligibleReasonCode,
            PersonIneligibleReasonGuid = person?.IneligibleReasonGuid,
            OnlineVoteRaw = vote.OnlineVoteRaw,
            SingleNameElectionCount = vote.SingleNameElectionCount,
            VoteStatusCode = vote.VoteStatus
        };
    }

    protected virtual bool IsSingleNameElection()
    {
        var electionType = TargetElection.ElectionType;
        return electionType == "Oth";
    }

    protected virtual void FillResultSummaryCalc()
    {
        ResultSummaryCalc.NumVoters = People.Count(p => !string.IsNullOrEmpty(p.VotingMethod));
        ResultSummaryCalc.NumEligibleToVote = People.Count(p => p.CanVote == true);

        ResultSummaryCalc.InPersonBallots = People.Count(p => p.VotingMethod == "P");
        ResultSummaryCalc.MailedInBallots = People.Count(p => p.VotingMethod == "M");
        ResultSummaryCalc.DroppedOffBallots = People.Count(p => p.VotingMethod == "D");
        ResultSummaryCalc.CalledInBallots = People.Count(p => p.VotingMethod == "C");
        ResultSummaryCalc.OnlineBallots = People.Count(p => p.VotingMethod == "O" || p.VotingMethod == "K");
        ResultSummaryCalc.ImportedBallots = People.Count(p => p.VotingMethod == "I");
        ResultSummaryCalc.Custom1Ballots = People.Count(p => p.VotingMethod == "1");
        ResultSummaryCalc.Custom2Ballots = People.Count(p => p.VotingMethod == "2");
        ResultSummaryCalc.Custom3Ballots = People.Count(p => p.VotingMethod == "3");
    }

    protected virtual void CalculateBallotStatistics()
    {
        Logger.LogInformation("Calculating ballot statistics");

        ResultSummaryCalc.BallotsNeedingReview = Ballots.Count(b => BallotAnalyzer.BallotNeedsReview(b.StatusCode));

        ResultSummaryCalc.TotalVotes = Ballots.Count * TargetElection.NumberToElect.Value;

        var invalidBallotGuids = Ballots
            .Where(b => b.StatusCode != BallotStatus.Ok)
            .Select(b => b.BallotGuid)
            .ToHashSet();

        ResultSummaryCalc.SpoiledBallots = invalidBallotGuids.Count;
        ResultSummaryCalc.BallotsReceived = Ballots.Count - ResultSummaryCalc.SpoiledBallots;

        ResultSummaryCalc.SpoiledVotes = Votes
            .Count(v => !invalidBallotGuids.Contains(v.BallotGuid) && v.VoteStatus != VoteStatus.Ok);

        Logger.LogInformation("Statistics: {TotalBallots} ballots ({SpoiledBallots} spoiled), {TotalVotes} potential votes ({SpoiledVotes} spoiled)",
            Ballots.Count, ResultSummaryCalc.SpoiledBallots, ResultSummaryCalc.TotalVotes, ResultSummaryCalc.SpoiledVotes);
    }

    protected abstract Task CountVotesAsync();

    protected virtual void FinalizeResultsAndTies()
    {
        Logger.LogInformation("Finalizing results and detecting ties");

        Results.RemoveAll(r => (r.VoteCount ?? 0) == 0);

        foreach (var result in Results)
        {
            if (_previousTieBreakCounts.TryGetValue(result.PersonGuid, out var tbc))
            {
                result.TieBreakCount = tbc;
            }
        }

        DetermineOrderAndSections();
        AnalyzeForTies();

        Logger.LogInformation("Finalized {ResultCount} results, {TieCount} tie groups",
            Results.Count, ResultTies.Count);
    }

    internal void DetermineOrderAndSections()
    {
        var numberToElect = TargetElection.NumberToElect.Value;
        var numberExtra = TargetElection.NumberExtra ?? 0;
        var ordinalRank = 0;
        var ordinalRankInExtra = 0;

        foreach (var result in Results
            .OrderByDescending(r => r.VoteCount)
            .ThenByDescending(r => r.TieBreakCount ?? 0)
            .ThenBy(r =>
            {
                var person = People.FirstOrDefault(p => p.PersonGuid == r.PersonGuid);
                return person?.FullNameFl ?? r.RowId.ToString();
            }))
        {
            ordinalRank++;
            result.Rank = ordinalRank;

            if (ordinalRank <= numberToElect)
            {
                result.Section = "E";
            }
            else if (ordinalRank <= numberToElect + numberExtra)
            {
                result.Section = "X";
            }
            else
            {
                result.Section = "O";
            }

            if (result.Section == "X")
            {
                ordinalRankInExtra++;
                result.RankInExtra = ordinalRankInExtra;
            }
        }
    }

    internal void AnalyzeForTies()
    {
        Result? aboveResult = null;
        var nextTieBreakGroup = 1;
        var foundFirstOneInOther = false;

        foreach (var result in Results.OrderBy(r => r.Rank))
        {
            result.IsTied = false;
            result.TieBreakGroup = null;
            result.ForceShowInOther = false;
            result.IsTieResolved = null;
            result.TieBreakRequired = false;

            if (aboveResult != null)
            {
                var numFewerVotes = (aboveResult.VoteCount ?? 0) - (result.VoteCount ?? 0);
                if (numFewerVotes == 0)
                {
                    aboveResult.IsTied = true;
                    result.IsTied = true;

                    if (!foundFirstOneInOther && result.Section == "O")
                    {
                        foundFirstOneInOther = true;
                    }

                    if (aboveResult.TieBreakGroup == null)
                    {
                        aboveResult.TieBreakGroup = nextTieBreakGroup;
                        nextTieBreakGroup++;
                    }
                    result.TieBreakGroup = aboveResult.TieBreakGroup;
                }
                else
                {
                    if (foundFirstOneInOther)
                    {
                        break;
                    }
                }

                var isClose = numFewerVotes <= ThresholdForCloseVote;
                aboveResult.CloseToNext = isClose;
                result.CloseToPrev = isClose;
            }
            else
            {
                result.CloseToPrev = false;
            }

            aboveResult = result;
        }

        if (aboveResult != null)
        {
            aboveResult.CloseToNext = false;
        }

        for (var groupCode = 1; groupCode < nextTieBreakGroup; groupCode++)
        {
            var code = groupCode;

            var resultTie = new ResultTie
            {
                ElectionGuid = TargetElection.ElectionGuid,
                TieBreakGroup = code,
            };

            ResultTies.Add(resultTie);

            AnalyzeTieGroup(resultTie, Results.Where(r => r.TieBreakGroup == code).OrderBy(r => r.Rank).ToList());
        }
    }

    private void AnalyzeTieGroup(ResultTie resultTie, List<Result> results)
    {
        if (results.Count == 0) return;

        resultTie.NumInTie = results.Count;
        resultTie.NumToElect = 0;
        resultTie.TieBreakRequired = false;

        var groupInTop = false;
        var groupInExtra = false;
        var groupInOther = false;

        foreach (var result in results)
        {
            switch (result.Section)
            {
                case "E":
                    groupInTop = true;
                    break;
                case "X":
                    groupInExtra = true;
                    break;
                case "O":
                    groupInOther = true;
                    break;
            }
        }

        var groupOnlyInTop = groupInTop && !(groupInExtra || groupInOther);
        var groupOnlyInOther = groupInOther && !(groupInTop || groupInExtra);
        var isResolved = true;

        foreach (var r in results)
        {
            r.TieBreakRequired = !(groupOnlyInOther || groupOnlyInTop);

            var stillTied = results.Any(other => other != r
                && (other.TieBreakCount ?? 0) == (r.TieBreakCount ?? 0)
                && (other.Section != r.Section || r.Section == "X"));

            if (stillTied)
            {
                isResolved = false;
            }
        }

        foreach (var r in results)
        {
            r.IsTieResolved = isResolved;
        }
        resultTie.IsResolved = isResolved;

        if (groupInOther && (groupInTop || groupInExtra))
        {
            foreach (var r in results.Where(r => r.Section == "O"))
            {
                r.ForceShowInOther = true;
            }
        }

        if (groupInTop)
        {
            if (!groupOnlyInTop)
            {
                resultTie.NumToElect += results.Count(r => r.Section == "E");
                resultTie.TieBreakRequired = true;
            }
        }

        if (groupInExtra)
        {
            resultTie.TieBreakRequired = true;

            if (!groupInTop)
            {
                resultTie.NumToElect += results.Count(r => r.Section == "X");
            }
        }

        if (resultTie.NumInTie == resultTie.NumToElect)
        {
            resultTie.NumToElect--;
        }

        if (resultTie.TieBreakRequired == true)
        {
            foreach (var r in results)
            {
                r.TieBreakCount ??= 0;
            }
        }
        else
        {
            foreach (var r in results)
            {
                r.TieBreakCount = null;
            }
        }
    }

    protected virtual void FinalizeSummaries()
    {
        Logger.LogInformation("Finalizing summaries");

        CombineCalcAndManualSummaries();

        var numBallotsWithManual = (ResultSummaryFinal.BallotsReceived ?? 0) + (ResultSummaryFinal.SpoiledBallots ?? 0);
        var sumOfEnvelopes = SumOfEnvelopesCollected(ResultSummaryFinal);

        ResultSummaryFinal.UseOnReports =
            ResultSummaryFinal.BallotsNeedingReview == 0
            && ResultTies.All(rt => rt.IsResolved == true)
            && numBallotsWithManual == sumOfEnvelopes;
    }

    protected void CombineCalcAndManualSummaries()
    {
        var manualSummaries = Context.ResultSummaries
            .Where(rs => rs.ElectionGuid == TargetElection.ElectionGuid && rs.ResultType == "M")
            .ToList();
        var manualOverride = manualSummaries.FirstOrDefault() ?? new ResultSummary();

        ResultSummaryFinal.NumEligibleToVote = manualOverride.NumEligibleToVote ?? ResultSummaryCalc.NumEligibleToVote ?? 0;
        ResultSummaryFinal.InPersonBallots = manualOverride.InPersonBallots ?? ResultSummaryCalc.InPersonBallots ?? 0;
        ResultSummaryFinal.DroppedOffBallots = manualOverride.DroppedOffBallots ?? ResultSummaryCalc.DroppedOffBallots ?? 0;
        ResultSummaryFinal.MailedInBallots = manualOverride.MailedInBallots ?? ResultSummaryCalc.MailedInBallots ?? 0;
        ResultSummaryFinal.CalledInBallots = manualOverride.CalledInBallots ?? ResultSummaryCalc.CalledInBallots ?? 0;
        ResultSummaryFinal.Custom1Ballots = manualOverride.Custom1Ballots ?? ResultSummaryCalc.Custom1Ballots ?? 0;
        ResultSummaryFinal.Custom2Ballots = manualOverride.Custom2Ballots ?? ResultSummaryCalc.Custom2Ballots ?? 0;
        ResultSummaryFinal.Custom3Ballots = manualOverride.Custom3Ballots ?? ResultSummaryCalc.Custom3Ballots ?? 0;
        ResultSummaryFinal.ImportedBallots = ResultSummaryCalc.ImportedBallots ?? 0;
        ResultSummaryFinal.OnlineBallots = ResultSummaryCalc.OnlineBallots ?? 0;

        ResultSummaryFinal.BallotsReceived = ResultSummaryCalc.BallotsReceived ?? 0;
        ResultSummaryFinal.NumVoters = manualOverride.NumVoters ?? ResultSummaryCalc.NumVoters ?? 0;
        ResultSummaryFinal.SpoiledManualBallots = manualOverride.SpoiledManualBallots;
        ResultSummaryFinal.BallotsNeedingReview = ResultSummaryCalc.BallotsNeedingReview;

        ResultSummaryFinal.SpoiledBallots =
            (manualOverride.SpoiledManualBallots ?? 0) + (ResultSummaryCalc.SpoiledBallots ?? 0);

        ResultSummaryFinal.SpoiledVotes = ResultSummaryCalc.SpoiledVotes;
        ResultSummaryFinal.TotalVotes = ResultSummaryCalc.TotalVotes;
    }

    protected static int SumOfEnvelopesCollected(ResultSummary rs)
    {
        if (rs.InPersonBallots.HasValue || rs.DroppedOffBallots.HasValue || rs.MailedInBallots.HasValue ||
            rs.CalledInBallots.HasValue || rs.OnlineBallots.HasValue || rs.ImportedBallots.HasValue ||
            rs.Custom1Ballots.HasValue || rs.Custom2Ballots.HasValue || rs.Custom3Ballots.HasValue)
        {
            return (rs.InPersonBallots ?? 0) + (rs.DroppedOffBallots ?? 0) + (rs.MailedInBallots ?? 0)
                   + (rs.CalledInBallots ?? 0) + (rs.OnlineBallots ?? 0) + (rs.ImportedBallots ?? 0)
                   + (rs.Custom1Ballots ?? 0) + (rs.Custom2Ballots ?? 0) + (rs.Custom3Ballots ?? 0);
        }
        return 0;
    }

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

    protected VoteStatus DetermineVoteStatus(Vote vote)
    {
        if (!string.IsNullOrEmpty(vote.OnlineVoteRaw)
            && vote.PersonGuid == null
            && string.IsNullOrEmpty(vote.IneligibleReasonCode))
        {
            return VoteStatus.Raw;
        }

        var person = People.FirstOrDefault(p => p.PersonGuid == vote.PersonGuid);

        if (person == null)
        {
            if (!string.IsNullOrEmpty(vote.IneligibleReasonCode))
                return VoteStatus.Spoiled;
            return VoteStatus.Spoiled;
        }

        if (person.CanReceiveVotes != true)
            return VoteStatus.Spoiled;

        if (!string.IsNullOrEmpty(person.CombinedInfo) &&
            !string.IsNullOrEmpty(vote.PersonCombinedInfo) &&
            !person.CombinedInfo.StartsWith(vote.PersonCombinedInfo))
        {
            return VoteStatus.Changed;
        }

        return VoteStatus.Ok;
    }
}



