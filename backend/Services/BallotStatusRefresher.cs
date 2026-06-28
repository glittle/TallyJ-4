using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Services.Analyzers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Re-evaluates a ballot's status from its current votes and election rules.
/// </summary>
public static class BallotStatusRefresher
{
    public static async Task RefreshAsync(
        MainDbContext context,
        Ballot ballot,
        ILogger? logger = null)
    {
        var electionGuid = ballot.Location?.ElectionGuid
            ?? await context.Locations
                .Where(l => l.LocationGuid == ballot.LocationGuid)
                .Select(l => l.ElectionGuid)
                .FirstOrDefaultAsync();

        if (electionGuid == Guid.Empty)
        {
            logger?.LogWarning(
                "Unable to refresh ballot status for {BallotGuid}: election not found",
                ballot.BallotGuid);
            return;
        }

        var election = await context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election?.NumberToElect is not int votesNeeded)
        {
            logger?.LogWarning(
                "Unable to refresh ballot status for {BallotGuid}: NumberToElect not configured",
                ballot.BallotGuid);
            return;
        }

        var votes = await context.Votes
            .Include(v => v.Person)
            .Where(v => v.BallotGuid == ballot.BallotGuid)
            .ToListAsync();

        var voteInfos = votes.Select(CreateBallotVoteInfo).ToList();
        var isSingleName = election.ElectionType == "Oth";
        var analyzer = new BallotAnalyzer(votesNeeded, isSingleName);

        analyzer.DetermineStatusFromVotes(ballot.StatusCode, voteInfos, out var newStatus, out _);
        ballot.StatusCode = newStatus;
    }

    private static BallotVoteInfo CreateBallotVoteInfo(Vote vote)
    {
        return new BallotVoteInfo
        {
            PersonGuid = vote.PersonGuid,
            PersonCanReceiveVotes = vote.Person?.CanReceiveVotes ?? false,
            PersonCombinedInfo = vote.Person?.CombinedInfo,
            VoteCombinedInfo = vote.PersonCombinedInfo,
            VoteIneligibleReasonCode = vote.IneligibleReasonCode,
            PersonIneligibleReasonGuid = vote.Person?.IneligibleReasonGuid,
            OnlineVoteRaw = vote.OnlineVoteRaw,
            SingleNameElectionCount = vote.SingleNameElectionCount,
            VoteStatusCode = vote.VoteStatus
        };
    }
}