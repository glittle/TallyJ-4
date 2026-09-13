using Backend.DTOs.Results;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class TallyService
{
    public const string ResultTypeCalculated = "C";
    public const string ResultTypeManual = "M";
    public const string ResultTypeFinal = "F";

    /// <summary>
    /// v3 Analyze count table. Calculated is the stored C row, or a live
    /// people-count when Analyze has not run yet. Manual nulls mean no override.
    /// Final for this panel is always Manual ?? Calculated so Save Values is
    /// visible before the next Calculate rewrites stored F.
    /// </summary>
    public async Task<AnalyzeCountSummariesDto> GetAnalyzeCountSummariesAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);
        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        var summaries = await _context.ResultSummaries
            .AsNoTracking()
            .Where(rs => rs.ElectionGuid == electionGuid)
            .ToListAsync();

        var calculated = summaries.FirstOrDefault(rs => rs.ResultType == ResultTypeCalculated);
        var manual = summaries.FirstOrDefault(rs => rs.ResultType == ResultTypeManual);

        var calculatedRow = calculated != null
            ? ToCountRow(calculated)
            : await LiveCalculatedCountsAsync(electionGuid);

        var manualRow = ToCountRow(manual);

        return new AnalyzeCountSummariesDto
        {
            Calculated = calculatedRow,
            Manual = manualRow,
            Final = CombineManualOverCalculated(calculatedRow, manualRow)
        };
    }

    /// <summary>
    /// v3 SaveManual: persist ResultType M overrides. Does not re-run Analyze
    /// (v4 Analyze stays behind the count-reconciliation gate). Next Calculate
    /// applies M via <c>CombineCalcAndManualSummaries</c>.
    /// </summary>
    public async Task<AnalyzeCountSummariesDto> SaveManualCountsAsync(
        Guid electionGuid,
        AnalyzeCountRowDto request)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);
        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        await ElectionFinalizedWriteGuard.ThrowIfLockedAsync(_context, electionGuid);

        var manual = await _context.ResultSummaries
            .FirstOrDefaultAsync(rs =>
                rs.ElectionGuid == electionGuid && rs.ResultType == ResultTypeManual);

        if (manual == null)
        {
            manual = new ResultSummary
            {
                ElectionGuid = electionGuid,
                ResultType = ResultTypeManual
            };
            _context.ResultSummaries.Add(manual);
        }

        manual.NumEligibleToVote = request.NumEligibleToVote;
        manual.InPersonBallots = request.InPersonBallots;
        manual.DroppedOffBallots = request.DroppedOffBallots;
        manual.MailedInBallots = request.MailedInBallots;
        manual.CalledInBallots = request.CalledInBallots;
        manual.Custom1Ballots = request.Custom1Ballots;
        manual.Custom2Ballots = request.Custom2Ballots;
        manual.Custom3Ballots = request.Custom3Ballots;
        manual.SpoiledManualBallots = request.SpoiledManualBallots;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Saved Analyze manual count overrides for election {ElectionGuid}",
            electionGuid);

        return await GetAnalyzeCountSummariesAsync(electionGuid);
    }

    private async Task<AnalyzeCountRowDto> LiveCalculatedCountsAsync(Guid electionGuid)
    {
        var people = await _context.People
            .AsNoTracking()
            .Where(p => p.ElectionGuid == electionGuid)
            .Select(p => new { p.CanVote, p.VotingMethod, p.PersonGuid })
            .ToListAsync();

        var processedOnline = await _context.OnlineVotingInfos
            .AsNoTracking()
            .Where(o => o.ElectionGuid == electionGuid && o.Status == OnlineBallotStatus.Processed)
            .Select(o => o.PersonGuid)
            .ToListAsync();
        var processedSet = processedOnline.ToHashSet();

        var breakdown = VotingMethodCodes.Count(people.Select(p =>
            (p.VotingMethod, processedSet.Contains(p.PersonGuid))));

        return new AnalyzeCountRowDto
        {
            NumEligibleToVote = people.Count(p => p.CanVote == true),
            InPersonBallots = breakdown.InPerson,
            DroppedOffBallots = breakdown.DroppedOff,
            MailedInBallots = breakdown.Mailed,
            CalledInBallots = breakdown.CalledIn,
            Custom1Ballots = breakdown.Custom1,
            Custom2Ballots = breakdown.Custom2,
            Custom3Ballots = breakdown.Custom3
        };
    }

    private static AnalyzeCountRowDto ToCountRow(ResultSummary? summary)
    {
        if (summary == null)
        {
            return new AnalyzeCountRowDto();
        }

        return new AnalyzeCountRowDto
        {
            NumEligibleToVote = summary.NumEligibleToVote,
            InPersonBallots = summary.InPersonBallots,
            DroppedOffBallots = summary.DroppedOffBallots,
            MailedInBallots = summary.MailedInBallots,
            CalledInBallots = summary.CalledInBallots,
            Custom1Ballots = summary.Custom1Ballots,
            Custom2Ballots = summary.Custom2Ballots,
            Custom3Ballots = summary.Custom3Ballots,
            SpoiledManualBallots = summary.SpoiledManualBallots
        };
    }

    private static AnalyzeCountRowDto CombineManualOverCalculated(
        AnalyzeCountRowDto calculated,
        AnalyzeCountRowDto manual)
    {
        return new AnalyzeCountRowDto
        {
            NumEligibleToVote = manual.NumEligibleToVote ?? calculated.NumEligibleToVote,
            InPersonBallots = manual.InPersonBallots ?? calculated.InPersonBallots,
            DroppedOffBallots = manual.DroppedOffBallots ?? calculated.DroppedOffBallots,
            MailedInBallots = manual.MailedInBallots ?? calculated.MailedInBallots,
            CalledInBallots = manual.CalledInBallots ?? calculated.CalledInBallots,
            Custom1Ballots = manual.Custom1Ballots ?? calculated.Custom1Ballots,
            Custom2Ballots = manual.Custom2Ballots ?? calculated.Custom2Ballots,
            Custom3Ballots = manual.Custom3Ballots ?? calculated.Custom3Ballots,
            SpoiledManualBallots = manual.SpoiledManualBallots ?? calculated.SpoiledManualBallots
        };
    }
}
