using Backend.Enumerations;

namespace Backend.Services.Analyzers;

/// <summary>
/// Ballot status from the votes on one ballot.
/// A normal ballot must name exactly <c>NumberToElect</c> people. Fewer is
/// <see cref="BallotStatus.TooFew"/>, more is <see cref="BallotStatus.TooMany"/>.
/// A single-name election (type <c>Oth</c>) skips that count: each vote row is
/// its own ballot. <see cref="BallotStatus.Review"/> is sticky and is not
/// replaced here. Online submit uses a different rule and does not call this.
/// </summary>
public class BallotAnalyzer
{
    private readonly int _numberToElect;
    private readonly bool _isSingleNameElection;

    public BallotAnalyzer(int numberToElect, bool isSingleNameElection)
    {
        _numberToElect = numberToElect;
        _isSingleNameElection = isSingleNameElection;
    }

    /// <summary>
    /// Chooses <paramref name="newStatus"/> from <paramref name="votes"/>.
    /// Returns true when that status differs from <paramref name="currentStatus"/>.
    /// <paramref name="spoiledVoteCount"/> is how many votes are already
    /// <see cref="VoteStatus.Spoiled"/>. Those votes still count toward the
    /// exact <c>NumberToElect</c> size; they do not by themselves spoil the ballot.
    /// Review wins over every other status and does not count as a change.
    /// </summary>
    public bool EvaluateBallotStatus(
        BallotStatus? currentStatus,
        List<BallotVoteInfo> votes,
        out BallotStatus newStatus,
        out int spoiledVoteCount)
    {
        spoiledVoteCount = votes.Count(v => v.VoteStatusCode == VoteStatus.Spoiled);

        if (currentStatus == BallotStatus.Review)
        {
            newStatus = BallotStatus.Review;
            return false;
        }

        // The person's current combined info should still start with the
        // snapshot stored on the vote. A null snapshot is compared as "NULL",
        // which a real name does not start with, so a missing snapshot is Verify.
        var nameChangedSinceVote = votes.Any(v =>
            !string.IsNullOrEmpty(v.PersonCombinedInfo) &&
            !v.PersonCombinedInfo.StartsWith(v.VoteCombinedInfo ?? "NULL"));
        if (nameChangedSinceVote)
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

        if (numVotes < _numberToElect)
        {
            return StatusChanged(BallotStatus.TooFew, currentStatus, out newStatus);
        }

        if (numVotes > _numberToElect)
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

    /// <summary>
    /// One vote, before the ballot-level count rules.
    /// An unmatched online raw name is <see cref="VoteStatus.Raw"/>.
    /// A person who cannot receive votes is <see cref="VoteStatus.Spoiled"/>.
    /// A name that no longer matches the snapshot is <see cref="VoteStatus.Changed"/>.
    /// </summary>
    public static VoteStatus DetermineVoteStatus(BallotVoteInfo vote)
    {
        if (!string.IsNullOrEmpty(vote.OnlineVoteRaw)
            && string.IsNullOrEmpty(vote.PersonIneligibleReasonCode)
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

    /// <summary>
    /// Statuses that block analysis as "needs review".
    /// Too few, too many, duplicate, and empty are not in this set. They are
    /// not Ok, so the summary counts them as spoiled ballots. The normal
    /// tally awards nothing from them. The single-name tally still counts
    /// an Ok vote on that row.
    /// </summary>
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

    /// <summary>Person.CombinedInfo now. Compared with <see cref="VoteCombinedInfo"/>.</summary>
    public string? PersonCombinedInfo { get; init; }

    /// <summary>Name snapshot stored on the vote when it was entered.</summary>
    public string? VoteCombinedInfo { get; init; }

    public string? VoteIneligibleReasonCode { get; init; }
    public string? PersonIneligibleReasonCode { get; init; }
    public string? OnlineVoteRaw { get; init; }
    public int? SingleNameElectionCount { get; init; }
    public VoteStatus VoteStatusCode { get; init; }
}
