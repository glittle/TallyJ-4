using Backend.Domain.Enumerations;

namespace Backend.Services.Analyzers;

public class BallotAnalyzer
{
    private readonly int _votesNeededOnBallot;
    private readonly bool _isSingleNameElection;

    public BallotAnalyzer(int votesNeededOnBallot, bool isSingleNameElection)
    {
        _votesNeededOnBallot = votesNeededOnBallot;
        _isSingleNameElection = isSingleNameElection;
    }

    public bool DetermineStatusFromVotes(
        BallotStatus? currentStatus,
        List<BallotVoteInfo> votes,
        out BallotStatus newStatus,
        out int spoiledCount)
    {
        spoiledCount = votes.Count(v => v.VoteStatusCode == VoteStatus.Spoiled);

        if (currentStatus == BallotStatus.Review)
        {
            newStatus = BallotStatus.Review;
            return false;
        }

        var needsVerification = votes.Any(v =>
            !string.IsNullOrEmpty(v.PersonCombinedInfo) &&
            !v.PersonCombinedInfo.StartsWith(v.VoteCombinedInfo ?? "NULL"));
        if (needsVerification)
        {
            return StatusChanged(BallotStatus.Verify, currentStatus, out newStatus);
        }

        if (votes.Any(v => v.VoteStatusCode == VoteStatus.Raw))
        {
            return StatusChanged(BallotStatus.Raw, currentStatus, out newStatus);
        }

        if (_isSingleNameElection)
        {
            return StatusChanged(BallotStatus.Ok, currentStatus, out newStatus);
        }

        var numVotes = votes.Count;

        if (numVotes == 0)
        {
            return StatusChanged(BallotStatus.Empty, currentStatus, out newStatus);
        }

        if (numVotes < _votesNeededOnBallot)
        {
            return StatusChanged(BallotStatus.TooFew, currentStatus, out newStatus);
        }

        if (numVotes > _votesNeededOnBallot)
        {
            return StatusChanged(BallotStatus.TooMany, currentStatus, out newStatus);
        }

        if (votes.Any(vote =>
            vote.PersonGuid.HasValue &&
            votes.Count(v => v.PersonGuid.HasValue && v.PersonGuid == vote.PersonGuid) > 1))
        {
            return StatusChanged(BallotStatus.Dup, currentStatus, out newStatus);
        }

        return StatusChanged(BallotStatus.Ok, currentStatus, out newStatus);
    }

    public static VoteStatus DetermineVoteStatus(BallotVoteInfo vote)
    {
        if (!string.IsNullOrEmpty(vote.OnlineVoteRaw)
            && vote.PersonIneligibleReasonGuid == null
            && vote.PersonGuid == null
            && string.IsNullOrEmpty(vote.VoteIneligibleReasonCode))
        {
            return VoteStatus.Raw;
        }

        if (!vote.PersonCanReceiveVotes)
        {
            return VoteStatus.Spoiled;
        }

        if (!string.IsNullOrEmpty(vote.PersonCombinedInfo) &&
            !vote.PersonCombinedInfo.StartsWith(vote.VoteCombinedInfo ?? "NULL"))
        {
            return VoteStatus.Changed;
        }

        return VoteStatus.Ok;
    }

    public static bool BallotNeedsReview(BallotStatus status)
    {
        return status == BallotStatus.Review
               || status == BallotStatus.Raw
               || status == BallotStatus.Verify;
    }

    private static bool StatusChanged(BallotStatus newStatusCode, BallotStatus? currentStatusCode, out BallotStatus finalStatusCode)
    {
        var isChanged = currentStatusCode != newStatusCode;
        finalStatusCode = newStatusCode;
        return isChanged;
    }
}

public record BallotVoteInfo
{
    public Guid? PersonGuid { get; init; }
    public bool PersonCanReceiveVotes { get; init; }
    public string? PersonCombinedInfo { get; init; }
    public string? VoteCombinedInfo { get; init; }
    public string? VoteIneligibleReasonCode { get; init; }
    public Guid? PersonIneligibleReasonGuid { get; init; }
    public string? OnlineVoteRaw { get; init; }
    public int? SingleNameElectionCount { get; init; }
    public VoteStatus VoteStatusCode { get; init; }
}
