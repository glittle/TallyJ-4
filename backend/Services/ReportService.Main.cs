using Backend.DTOs.Reports;
using Backend.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class ReportService
{
    public async Task<MainReportDto> GetMainReportAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var summary = await _context.ResultSummaries
            .Where(rs => rs.ElectionGuid == electionGuid && rs.ResultType == "F")
            .FirstOrDefaultAsync();

        var numToElect = election.NumberToElect ?? 0;
        var numExtra = election.NumberExtra ?? 0;

        var results = await _context.Results
            .Include(r => r.Person)
            .Where(r => r.ElectionGuid == electionGuid)
            .OrderBy(r => r.Rank)
            .Take(numToElect + numExtra)
            .ToListAsync();

        var elected = results.Select(r => new ElectedPersonDto
        {
            Rank = r.Section == "X"
                ? "Next " + r.RankInExtra
                : r.Rank.ToString(),
            Name = r.Person?.FullName ?? "",
            BahaiId = r.Person?.BahaiId,
            VoteCountDisplay = (r.VoteCount ?? 0).ToString("N0") +
                               (r.TieBreakRequired == true ? " / " + r.TieBreakCount : ""),
            Section = r.Section
        }).ToList();

        var ballots = await _context.Ballots
            .Include(b => b.Location)
            .Where(b => b.Location.ElectionGuid == electionGuid && b.StatusCode != BallotStatus.Ok)
            .ToListAsync();

        var spoiledBallotReasons = ballots
            .GroupBy(b => b.StatusCode)
            .Select(g => new SpoiledBallotGroupDto
            {
                Reason = BallotStatusEnum.GetDescription(g.Key) ?? g.Key.ToString(),
                BallotCount = g.Count()
            })
            .OrderByDescending(x => x.BallotCount)
            .ToList();

        if (summary?.SpoiledManualBallots > 0)
        {
            spoiledBallotReasons.Add(new SpoiledBallotGroupDto
            {
                Reason = "Unknown (Manual Count)",
                BallotCount = summary.SpoiledManualBallots.Value
            });
        }

        var votes = await _context.Votes
            .Include(v => v.Person)
            .Where(v => _context.Ballots.Any(b =>
                b.BallotGuid == v.BallotGuid &&
                b.StatusCode == BallotStatus.Ok &&
                b.Location.ElectionGuid == electionGuid))
            .ToListAsync();
        var spoiledVoteReasons = votes
            .Where(v =>
            {
                if (v.IneligibleReasonCode != null) return true;
                if (v.Person != null && v.Person.CanReceiveVotes != true && v.Person.IneligibleReasonGuid != null) return true;
                return false;
            })
            .GroupBy(v =>
            {
                if (v.IneligibleReasonCode != null) return v.IneligibleReasonCode;
                return v.Person?.IneligibleReasonGuid?.ToString() ?? "";
            })
            .Select(g =>
            {
                var desc = GetIneligibleDescription(g.First().IneligibleReasonCode)
                           ?? GetIneligibleDescriptionByGuid(g.First().Person?.IneligibleReasonGuid);
                return new SpoiledVoteGroupDto
                {
                    Reason = desc ?? "Unknown",
                    VoteCount = g.Count()
                };
            })
            .Where(x => !string.IsNullOrEmpty(x.Reason) && x.Reason != "Unknown")
            .OrderByDescending(x => x.VoteCount)
            .ThenBy(x => x.Reason)
            .ToList();

        var numEligible = summary?.NumEligibleToVote ?? 0;
        var numVoted = summary?.NumVoters ?? 0;
        var totalBallots = summary?.BallotsReceived ?? 0;
        var participation = numEligible > 0 ? (double)numVoted / numEligible * 100 : 0;

        return new MainReportDto
        {
            ElectionName = election.Name,
            Convenor = election.Convenor,
            DateOfElection = election.DateOfElection,
            NumEligibleToVote = numEligible,
            SumOfEnvelopesCollected = numVoted,
            NumBallotsWithManual = totalBallots,
            PercentParticipation = participation,
            InPersonBallots = summary?.InPersonBallots ?? 0,
            MailedInBallots = summary?.MailedInBallots ?? 0,
            DroppedOffBallots = summary?.DroppedOffBallots ?? 0,
            OnlineBallots = summary?.OnlineBallots ?? 0,
            ImportedBallots = summary?.ImportedBallots ?? 0,
            CalledInBallots = summary?.CalledInBallots ?? 0,
            Custom1Ballots = summary?.Custom1Ballots ?? 0,
            Custom2Ballots = summary?.Custom2Ballots ?? 0,
            Custom3Ballots = summary?.Custom3Ballots ?? 0,
            Custom1Name = ParseCustomMethodName(election.CustomMethods, 0),
            Custom2Name = ParseCustomMethodName(election.CustomMethods, 1),
            Custom3Name = ParseCustomMethodName(election.CustomMethods, 2),
            SpoiledBallots = summary?.SpoiledBallots ?? 0,
            SpoiledVotes = summary?.SpoiledVotes ?? 0,
            SpoiledBallotReasons = spoiledBallotReasons,
            SpoiledVoteReasons = spoiledVoteReasons,
            Elected = elected,
            HasTies = elected.Any(e => e.VoteCountDisplay.Contains('/'))
        };
    }
}
