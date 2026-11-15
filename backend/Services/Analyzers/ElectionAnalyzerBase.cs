using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Analyzers;

/// <summary>
/// One tally pass. Clears calculated results, refreshes ballot and vote
/// status, writes a calculated summary (<c>C</c>) and the report summary
/// (<c>F</c>, calculated counts with a manual <c>M</c> overlay), then ranks
/// and detects ties. Only an <see cref="BallotStatus.Ok"/> ballot awards
/// votes. Entered ballots are Ok plus every other status.
/// </summary>
public abstract class ElectionAnalyzerBase
{
    // A gap of this many votes or fewer sets CloseToNext / CloseToPrev.
    // A gap of 0 is also a tie.
    private const int ThresholdForCloseVote = 3;

    /// <summary>Stored <c>Result.Section</c>. Not the localized display word.</summary>
    protected const string ElectedSection = "E";

    /// <summary>Stored <c>Result.Section</c> for the extra seats.</summary>
    protected const string ExtraSection = "X";

    /// <summary>Stored <c>Result.Section</c> for everyone else.</summary>
    protected const string OtherSection = "O";

    protected readonly MainDbContext Context;
    protected readonly ILogger Logger;
    protected readonly Election TargetElection;

    protected List<Ballot> Ballots = new();
    protected List<Vote> Votes = new();
    protected List<Person> People = new();
    protected HashSet<Guid> ProcessedOnlinePersonGuids = new();
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

        var strategy = Context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
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
        });
    }

    protected virtual async Task PrepareForAnalysisAsync()
    {
        Logger.LogInformation("Preparing for analysis of election {ElectionGuid}", TargetElection.ElectionGuid);

        var existingResults = await Context.Results
            .Where(r => r.ElectionGuid == TargetElection.ElectionGuid)
            .ToListAsync();

        // Taken before the rows are deleted. A re-analysis puts these back
        // so a saved runoff, including an explicit 0, is not wiped.
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
            .Where(rs => rs.ElectionGuid == TargetElection.ElectionGuid && rs.ResultType != TallyService.ResultTypeManual)
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

        ProcessedOnlinePersonGuids = (await Context.OnlineVotingInfos
                .AsNoTracking()
                .Where(o => o.ElectionGuid == TargetElection.ElectionGuid
                            && o.Status == OnlineBallotStatus.Processed)
                .Select(o => o.PersonGuid)
                .ToListAsync())
            .ToHashSet();

        Results = new List<Result>();
        ResultTies = new List<ResultTie>();

        ResultSummaryCalc = new ResultSummary
        {
            ElectionGuid = TargetElection.ElectionGuid,
            ResultType = TallyService.ResultTypeCalculated
        };
        Context.ResultSummaries.Add(ResultSummaryCalc);

        ResultSummaryFinal = new ResultSummary
        {
            ElectionGuid = TargetElection.ElectionGuid,
            ResultType = TallyService.ResultTypeFinal
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
        var numberToElect = TargetElection.NumberToElect!.Value;
        var ballotAnalyzer = new BallotAnalyzer(numberToElect, isSingleName);

        // Pre-group votes by BallotGuid for O(1) lookup instead of O(votes) per ballot
        var votesByBallotGuid = Votes.ToLookup(v => v.BallotGuid);

        foreach (var ballot in Ballots)
        {
            var ballotVotes = votesByBallotGuid[ballot.BallotGuid].ToList();
            var voteInfos = ballotVotes.Select(v => CreateBallotVoteInfo(v)).ToList();
            ballotAnalyzer.EvaluateBallotStatus(ballot.StatusCode, voteInfos, out var newStatus, out _);
            ballot.StatusCode = newStatus;
        }
    }

    protected BallotVoteInfo CreateBallotVoteInfo(Vote vote)
    {
        var person = People.FirstOrDefault(p => p.PersonGuid == vote.PersonGuid);
        return new BallotVoteInfo
        {
            PersonGuid = vote.PersonGuid,
            PersonCanReceiveVotes = PersonEligibilityHelper.CanReceiveVotes(person),
            PersonCombinedInfo = person?.CombinedInfo,
            VoteCombinedInfo = vote.PersonCombinedInfo,
            VoteIneligibleReasonCode = vote.IneligibleReasonCode,
            PersonIneligibleReasonCode = person?.IneligibleReasonCode,
            OnlineVoteRaw = vote.OnlineVoteRaw,
            SingleNameElectionCount = vote.SingleNameElectionCount,
            VoteStatusCode = vote.VoteStatus
        };
    }

    /// <summary>
    /// Election type <c>Oth</c> ("Other") is the single-name ballot.
    /// Each vote row is its own ballot, so <c>NumberToElect</c> is not
    /// required on the paper. <see cref="ElectionAnalyzerSingleName"/> always
    /// returns true, including when the stored type is not <c>Oth</c>.
    /// </summary>
    protected virtual bool IsSingleNameElection()
    {
        return TargetElection.ElectionType == ElectionTypeEnum.Oth.Code;
    }

    protected virtual void FillResultSummaryCalc()
    {
        // People by Front Desk method, plus a processed online row. These are
        // not ballot rows. Ballot rows are counted in CalculateBallotStatistics.
        var breakdown = VotingMethodCodes.Count(People.Select(p =>
            (p.VotingMethod, ProcessedOnlinePersonGuids.Contains(p.PersonGuid))));

        ResultSummaryCalc.NumVoters = People.Count(p =>
            VotingMethodCodes.HasVotedForCounts(
                p.VotingMethod,
                ProcessedOnlinePersonGuids.Contains(p.PersonGuid)));
        ResultSummaryCalc.NumEligibleToVote = People.Count(p => p.CanVote == true);

        ResultSummaryCalc.InPersonBallots = breakdown.InPerson;
        ResultSummaryCalc.MailedInBallots = breakdown.Mailed;
        ResultSummaryCalc.DroppedOffBallots = breakdown.DroppedOff;
        ResultSummaryCalc.CalledInBallots = breakdown.CalledIn;
        ResultSummaryCalc.OnlineBallots = breakdown.Online + breakdown.Kiosk;
        ResultSummaryCalc.ImportedBallots = breakdown.Imported;
        ResultSummaryCalc.Custom1Ballots = breakdown.Custom1;
        ResultSummaryCalc.Custom2Ballots = breakdown.Custom2;
        ResultSummaryCalc.Custom3Ballots = breakdown.Custom3;
    }

    protected virtual void CalculateBallotStatistics()
    {
        Logger.LogInformation("Calculating ballot statistics");

        ResultSummaryCalc.BallotsNeedingReview = Ballots.Count(b => BallotAnalyzer.BallotNeedsReview(b.StatusCode));

        // Potential seats: every ballot × Number to Elect, including ballots
        // that will not award a vote. CountVotesAsync counts only Ok ballots.
        ResultSummaryCalc.TotalVotes = Ballots.Count * TargetElection.NumberToElect!.Value;

        // Entered ballots = Ok + every other status. BallotsReceived is the Ok
        // count. SpoiledBallots is the rest (empty, too few, too many,
        // duplicate, review, raw, verify), not only a teller "spoiled" mark.
        // Reports add the two fields back together.
        var notOkBallotGuids = Ballots
            .Where(b => b.StatusCode != BallotStatus.Ok)
            .Select(b => b.BallotGuid)
            .ToHashSet();

        ResultSummaryCalc.SpoiledBallots = notOkBallotGuids.Count;
        ResultSummaryCalc.BallotsReceived = Ballots.Count - ResultSummaryCalc.SpoiledBallots;

        // A non-Ok vote on an Ok ballot. Votes on a not-Ok ballot are not
        // counted again here; that ballot is already in SpoiledBallots.
        ResultSummaryCalc.SpoiledVotes = Votes
            .Count(v => !notOkBallotGuids.Contains(v.BallotGuid) && v.VoteStatus != VoteStatus.Ok);

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
        var numberToElect = TargetElection.NumberToElect!.Value;
        var numberExtra = TargetElection.NumberExtra ?? 0;
        var ordinalRank = 0;
        var ordinalRankInExtra = 0;

        foreach (var result in Results
            .OrderByDescending(r => r.VoteCount)
            .ThenByDescending(r => r.TieBreakCount ?? 0) // unset (null) ranks the same as an explicit 0
            .ThenBy(r =>
            {
                var person = People.FirstOrDefault(p => p.PersonGuid == r.PersonGuid);
                return person != null ? PersonNameHelper.GetSortKey(person) : r.RowId.ToString();
            }))
        {
            ordinalRank++;
            result.Rank = ordinalRank;

            if (ordinalRank <= numberToElect)
            {
                result.Section = ElectedSection;
            }
            else if (ordinalRank <= numberToElect + numberExtra)
            {
                result.Section = ExtraSection;
            }
            else
            {
                result.Section = OtherSection;
            }

            if (result.Section == ExtraSection)
            {
                ordinalRankInExtra++;
                result.RankInExtra = ordinalRankInExtra;
            }
        }
    }

    internal void AnalyzeForTies()
    {
        Result? higherRanked = null;
        var nextTieBreakGroup = 1;
        // Once a tie includes Other, the next lower total ends the scan.
        // Names further down are not a reportable tie.
        var tieReachedOtherSection = false;

        foreach (var result in Results.OrderBy(r => r.Rank))
        {
            result.IsTied = false;
            result.TieBreakGroup = null;
            result.ForceShowInOther = false;
            result.IsTieResolved = null;
            result.TieBreakRequired = false;

            if (higherRanked != null)
            {
                var numFewerVotes = (higherRanked.VoteCount ?? 0) - (result.VoteCount ?? 0);
                if (numFewerVotes == 0)
                {
                    higherRanked.IsTied = true;
                    result.IsTied = true;

                    if (!tieReachedOtherSection && result.Section == OtherSection)
                    {
                        tieReachedOtherSection = true;
                    }

                    if (higherRanked.TieBreakGroup == null)
                    {
                        higherRanked.TieBreakGroup = nextTieBreakGroup;
                        nextTieBreakGroup++;
                    }
                    result.TieBreakGroup = higherRanked.TieBreakGroup;
                }
                else if (tieReachedOtherSection)
                {
                    break;
                }

                var isClose = numFewerVotes <= ThresholdForCloseVote;
                higherRanked.CloseToNext = isClose;
                result.CloseToPrev = isClose;
            }
            else
            {
                result.CloseToPrev = false;
            }

            higherRanked = result;
        }

        if (higherRanked != null)
        {
            higherRanked.CloseToNext = false;
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

        var inElected = false;
        var groupInExtra = false;
        var groupInOther = false;

        foreach (var result in results)
        {
            switch (result.Section)
            {
                case ElectedSection:
                    inElected = true;
                    break;
                case ExtraSection:
                    groupInExtra = true;
                    break;
                case OtherSection:
                    groupInOther = true;
                    break;
            }
        }

        var onlyInElected = inElected && !(groupInExtra || groupInOther);
        // Unset (null) and explicit 0 compare equal; all-0 and all-unset stay unresolved.
        var needsTieBreak = TieBreakResolution.NeedsTieBreak(results);
        var isResolved = !needsTieBreak || TieBreakResolution.CountsDecidePositions(results);

        foreach (var r in results)
        {
            r.TieBreakRequired = needsTieBreak;
        }

        foreach (var r in results)
        {
            r.IsTieResolved = isResolved;
        }
        resultTie.IsResolved = isResolved;

        if (groupInOther && (inElected || groupInExtra))
        {
            foreach (var r in results.Where(r => r.Section == OtherSection))
            {
                r.ForceShowInOther = true;
            }
        }

        if (inElected && !onlyInElected)
        {
            resultTie.NumToElect += results.Count(r => r.Section == ElectedSection);
            resultTie.TieBreakRequired = true;
        }

        if (groupInExtra)
        {
            resultTie.TieBreakRequired = true;

            if (!inElected)
            {
                resultTie.NumToElect += results.Count(r => r.Section == ExtraSection);
            }
        }

        // An all-Extra tie set NumToElect to the whole group. One name does
        // not get an extra seat, so the runoff has to identify that name.
        // A tie that already crosses into Elected counted only the elected
        // seats, which is already smaller than the group.
        if (resultTie.NumInTie == resultTie.NumToElect)
        {
            resultTie.NumToElect--;
        }

        if (resultTie.TieBreakRequired == true && isResolved)
        {
            // The runoff distinguished this group. "Tied" and "Tie-break required"
            // both mean it is still unresolved. Keep the counts so the results table
            // and reports can show them; a later Calculate must not put the flags back.
            resultTie.TieBreakRequired = false;
            foreach (var r in results)
            {
                r.TieBreakRequired = false;
                r.IsTied = false;
            }
        }
        // Entered counts stay, including an explicit 0, even when this group does not
        // need a runoff (everyone tied is already elected, or the tie is only in Other).
        // Required ties keep null until a teller enters a value. Sorting and still-tied
        // checks treat null as 0 without collapsing the two.
    }

    protected virtual void FinalizeSummaries()
    {
        Logger.LogInformation("Finalizing summaries");

        CombineCalcAndManualSummaries();

        // Ok ballots + not-Ok ballots, after manual spoiled are included.
        // That total has to match the Front Desk method counts (the
        // "envelopes") before the summary is the one reports use.
        var enteredBallots = (ResultSummaryFinal.BallotsReceived ?? 0) + (ResultSummaryFinal.SpoiledBallots ?? 0);
        var envelopesCollected = SumOfEnvelopesCollected(ResultSummaryFinal);

        ResultSummaryFinal.UseOnReports =
            ResultSummaryFinal.BallotsNeedingReview == 0
            && ResultTies.All(rt => rt.IsResolved == true)
            && enteredBallots == envelopesCollected;
    }

    protected void CombineCalcAndManualSummaries()
    {
        // Manual (M) can replace the people and method counts. It does not
        // replace the calculated Ok / not-Ok split. A manual spoiled count is
        // added to the calculated spoiled ballots. Online and imported stay
        // on the calculated numbers.
        var manualSummaries = Context.ResultSummaries
            .Where(rs => rs.ElectionGuid == TargetElection.ElectionGuid && rs.ResultType == TallyService.ResultTypeManual)
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

    /// <summary>
    /// Vote status for this tally pass. A missing person is spoiled.
    /// <see cref="BallotAnalyzer.DetermineVoteStatus"/> is the entry-time
    /// check: it also treats a null name snapshot as changed, and it does
    /// not look up the person list. The two are not the same method.
    /// </summary>
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
            // No matching person: the name is not in the list. It cannot be counted.
            return VoteStatus.Spoiled;
        }

        if (!PersonEligibilityHelper.CanReceiveVotes(person))
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



