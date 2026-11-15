using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services.Analyzers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Re-evaluates a ballot's status from its current votes and election rules.
/// </summary>
public static class BallotStatusRefresher
{
    /// <summary>
    /// Re-evaluates every ballot status in an election from current votes.
    /// </summary>
    public static async Task RefreshForElectionAsync(
        MainDbContext context,
        Guid electionGuid,
        ILogger? logger = null)
    {
        var election = await context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election?.NumberToElect is not int numberToElect)
        {
            logger?.LogWarning(
                "Unable to refresh ballot statuses for election {ElectionGuid}: NumberToElect not configured",
                electionGuid);
            return;
        }

        var ballots = await context.Ballots
            .Include(b => b.Location)
            .Include(b => b.Votes)
                .ThenInclude(v => v.Person)
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .ToListAsync();

        // Oth is the single-name election: the exact NumberToElect count is not applied.
        var isSingleName = election.ElectionType == ElectionTypeEnum.Oth.Code;
        var analyzer = new BallotAnalyzer(numberToElect, isSingleName);

        foreach (var ballot in ballots)
        {
            var voteInfos = ballot.Votes.Select(CreateBallotVoteInfo).ToList();
            analyzer.EvaluateBallotStatus(ballot.StatusCode, voteInfos, out var newStatus, out _);
            if (ballot.StatusCode != newStatus)
            {
                ballot.StatusCode = newStatus;
                ballot.DateUpdated = DateTimeOffset.UtcNow;
            }
        }
    }

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

        if (election?.NumberToElect is not int numberToElect)
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
        var isSingleName = election.ElectionType == ElectionTypeEnum.Oth.Code;
        var analyzer = new BallotAnalyzer(numberToElect, isSingleName);

        analyzer.EvaluateBallotStatus(ballot.StatusCode, voteInfos, out var newStatus, out _);
        ballot.StatusCode = newStatus;
    }

    private static BallotVoteInfo CreateBallotVoteInfo(Vote vote)
    {
        return new BallotVoteInfo
        {
            PersonGuid = vote.PersonGuid,
            PersonCanReceiveVotes = PersonEligibilityHelper.CanReceiveVotes(vote.Person),
            PersonCombinedInfo = vote.Person?.CombinedInfo,
            VoteCombinedInfo = vote.PersonCombinedInfo,
            VoteIneligibleReasonCode = vote.IneligibleReasonCode,
            PersonIneligibleReasonCode = vote.Person?.IneligibleReasonCode,
            OnlineVoteRaw = vote.OnlineVoteRaw,
            SingleNameElectionCount = vote.SingleNameElectionCount,
            VoteStatusCode = vote.VoteStatus
        };
    }
}