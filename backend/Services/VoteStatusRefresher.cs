using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Services.Analyzers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Re-evaluates vote statuses when a person's eligibility changes.
/// </summary>
public static class VoteStatusRefresher
{
    /// <summary>
    /// Re-evaluates all votes in an election against current person eligibility.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="electionGuid">The election whose votes should be re-evaluated.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <returns>The number of vote fields whose status or ineligible reason changed.</returns>
    public static async Task<int> RefreshVotesForElectionAsync(
        MainDbContext context,
        Guid electionGuid,
        ILogger? logger = null)
    {
        var votes = await context.Votes
            .Include(v => v.Person)
            .Include(v => v.Ballot)
                .ThenInclude(b => b.Location)
            .Where(v => v.Ballot.Location.ElectionGuid == electionGuid)
            .ToListAsync();

        if (votes.Count == 0)
        {
            return 0;
        }

        var changedCount = 0;

        foreach (var vote in votes)
        {
            changedCount += ApplyCurrentEligibility(vote, vote.Person);
        }

        if (changedCount > 0)
        {
            logger?.LogInformation(
                "Re-evaluated {VoteCount} vote(s) for election {ElectionGuid}; {ChangedCount} field(s) updated",
                votes.Count, electionGuid, changedCount);
        }

        return changedCount;
    }

    /// <returns>The number of votes whose status changed.</returns>
    public static async Task<int> RefreshVotesForPersonAsync(
        MainDbContext context,
        Person person,
        ILogger? logger = null)
    {
        var votes = await context.Votes
            .Include(v => v.Person)
            .Include(v => v.Ballot)
                .ThenInclude(b => b.Location)
            .Where(v => v.PersonGuid == person.PersonGuid)
            .ToListAsync();

        if (votes.Count == 0)
        {
            return 0;
        }

        var changedCount = 0;
        var affectedBallots = new HashSet<Ballot>();

        foreach (var vote in votes)
        {
            changedCount += ApplyCurrentEligibility(vote, person);
            affectedBallots.Add(vote.Ballot);
        }

        foreach (var ballot in affectedBallots)
        {
            ballot.DateUpdated = DateTimeOffset.UtcNow;
            await BallotStatusRefresher.RefreshAsync(context, ballot, logger);
        }

        if (changedCount > 0)
        {
            logger?.LogInformation(
                "Re-evaluated {VoteCount} vote(s) for person {PersonGuid}; {ChangedCount} status(es) updated",
                votes.Count, person.PersonGuid, changedCount);
        }

        return changedCount;
    }

    private static int ApplyCurrentEligibility(Vote vote, Person? person)
    {
        var changedCount = 0;
        var newStatus = BallotAnalyzer.DetermineVoteStatus(CreateBallotVoteInfo(vote, person));
        var newReasonCode = person == null
            ? vote.IneligibleReasonCode
            : person.CanReceiveVotes != true
                ? GetIneligibleReasonCode(person.IneligibleReasonGuid)
                : null;

        if (vote.VoteStatus != newStatus)
        {
            vote.VoteStatus = newStatus;
            changedCount++;
        }

        if (vote.IneligibleReasonCode != newReasonCode)
        {
            vote.IneligibleReasonCode = newReasonCode;
            changedCount++;
        }

        return changedCount;
    }

    private static string? GetIneligibleReasonCode(Guid? guid)
    {
        return guid.HasValue ? IneligibleReasonEnum.GetByGuid(guid.Value)?.Code : null;
    }

    private static BallotVoteInfo CreateBallotVoteInfo(Vote vote, Person? person)
    {
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
}