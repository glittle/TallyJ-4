using Backend.Domain.Enumerations;
using Backend.Services.Analyzers;

namespace Backend.Tests.UnitTests;

public class BallotAnalyzerTests
{
    [Fact]
    public void CorrectNumberOfVotes()
    {
        var voteInfos = new List<BallotVoteInfo>
        {
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
        };

        var model = new BallotAnalyzer(3, false);

        var changed = model.DetermineStatusFromVotes(null, voteInfos, out var newStatus, out var spoiledCount);

        Assert.True(changed);
        Assert.Equal(BallotStatus.Ok, newStatus);
        Assert.Equal(0, spoiledCount);
    }

    [Fact]
    public void TooManyNumberOfVotes()
    {
        var voteInfos = new List<BallotVoteInfo>
        {
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
        };

        var model = new BallotAnalyzer(3, false);

        var changed = model.DetermineStatusFromVotes(null, voteInfos, out var newStatus, out var spoiledCount);

        Assert.True(changed);
        Assert.Equal(BallotStatus.TooMany, newStatus);
        Assert.Equal(0, spoiledCount);
    }

    [Fact]
    public void TooManyNumberOfVotesWithBlank()
    {
        var votes = new List<BallotVoteInfo>
        {
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
        };

        var model = new BallotAnalyzer(3, false);

        var changed = model.DetermineStatusFromVotes(null, votes, out var newStatus, out var spoiledCount);

        Assert.True(changed);
        Assert.Equal(BallotStatus.Ok, newStatus);
        Assert.Equal(0, spoiledCount);
    }

    [Fact]
    public void TooManyNumberOfVotesWithIneligible()
    {
        var votes = new List<BallotVoteInfo>
        {
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
            new() { VoteIneligibleReasonCode = "U01" },
        };

        var model = new BallotAnalyzer(3, false);

        var changed = model.DetermineStatusFromVotes(null, votes, out var newStatus, out var spoiledCount);

        Assert.True(changed);
        Assert.Equal(BallotStatus.TooMany, newStatus);
        Assert.Equal(0, spoiledCount);
    }

    [Fact]
    public void TooManyNumberOfVotesWithSpoiled()
    {
        var votes = new List<BallotVoteInfo>
        {
            new() { VoteIneligibleReasonCode = "U01" },
            new() { VoteIneligibleReasonCode = "U01" },
            new() { VoteIneligibleReasonCode = "U01" },
            new() { VoteIneligibleReasonCode = "U01" },
        };

        var model = new BallotAnalyzer(3, false);

        var changed = model.DetermineStatusFromVotes(null, votes, out var newStatus, out var spoiledCount);

        Assert.True(changed);
        Assert.Equal(BallotStatus.TooMany, newStatus);
        Assert.Equal(0, spoiledCount);
    }

    [Fact]
    public void TooFewNumberOfVotes()
    {
        var votes = new List<BallotVoteInfo>
        {
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
        };

        var model = new BallotAnalyzer(3, false);

        var changed = model.DetermineStatusFromVotes(null, votes, out var newStatus, out _);

        Assert.True(changed);
        Assert.Equal(BallotStatus.TooFew, newStatus);
    }

    [Fact]
    public void SingleIneligible()
    {
        var votes = new List<BallotVoteInfo>
        {
            new()
            {
                VoteIneligibleReasonCode = "U02",
                SingleNameElectionCount = 4
            },
        };

        var model = new BallotAnalyzer(1, true);

        var changed = model.DetermineStatusFromVotes(null, votes, out var newStatus, out _);

        Assert.True(changed);
        Assert.Equal(BallotStatus.Ok, newStatus);
    }

    [Fact]
    public void EmptyNumberOfVotes()
    {
        var votes = new List<BallotVoteInfo>();

        var model = new BallotAnalyzer(3, false);

        var changed = model.DetermineStatusFromVotes(null, votes, out var newStatus, out _);

        Assert.True(changed);
        Assert.Equal(BallotStatus.Empty, newStatus);
    }

    [Fact]
    public void TooFewNumberOfVotesWithBlank()
    {
        var votes = new List<BallotVoteInfo>
        {
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
        };

        var model = new BallotAnalyzer(3, false);

        var changed = model.DetermineStatusFromVotes(null, votes, out var newStatus, out var spoiledCount);

        Assert.True(changed);
        Assert.Equal(BallotStatus.TooFew, newStatus);
        Assert.Equal(0, spoiledCount);
    }

    [Fact]
    public void KeepReviewStatus()
    {
        var votes = new List<BallotVoteInfo>
        {
            new() { PersonGuid = Guid.NewGuid() },
        };

        var model = new BallotAnalyzer(3, false);

        var changed = model.DetermineStatusFromVotes(BallotStatus.Review, votes, out var newStatus, out _);
        Assert.False(changed);
        Assert.Equal(BallotStatus.Review, newStatus);

        votes = new List<BallotVoteInfo>
        {
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
        };

        changed = model.DetermineStatusFromVotes(BallotStatus.Review, votes, out newStatus, out _);
        Assert.False(changed);
        Assert.Equal(BallotStatus.Review, newStatus);
    }

    [Fact]
    public void HasDuplicates()
    {
        var dupPersonGuid = Guid.NewGuid();

        var votes = new List<BallotVoteInfo>
        {
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = dupPersonGuid },
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = dupPersonGuid },
        };

        var model = new BallotAnalyzer(5, false);

        var changed = model.DetermineStatusFromVotes(BallotStatus.Ok, votes, out var newStatus, out _);

        Assert.True(changed);
        Assert.Equal(BallotStatus.Dup, newStatus);
    }

    [Fact]
    public void HasDuplicatesAndTooMany()
    {
        var dupPersonGuid = Guid.NewGuid();

        var votes = new List<BallotVoteInfo>
        {
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = dupPersonGuid },
            new() { PersonGuid = dupPersonGuid },
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
            new() { PersonGuid = Guid.NewGuid() },
        };

        var model = new BallotAnalyzer(5, false);

        var changed = model.DetermineStatusFromVotes(BallotStatus.Ok, votes, out var newStatus, out _);

        Assert.True(changed);
        Assert.Equal(BallotStatus.TooMany, newStatus);
    }

    [Fact]
    public void AllSpoiled()
    {
        var votes = new List<BallotVoteInfo>
        {
            new() { PersonGuid = Guid.NewGuid(), PersonCanReceiveVotes = false, VoteStatusCode = VoteStatus.Spoiled },
            new() { VoteIneligibleReasonCode = "U01", VoteStatusCode = VoteStatus.Spoiled },
            new() { PersonGuid = Guid.NewGuid(), PersonCanReceiveVotes = false, VoteStatusCode = VoteStatus.Spoiled },
        };

        var model = new BallotAnalyzer(3, false);

        var changed = model.DetermineStatusFromVotes(BallotStatus.Ok, votes, out var newStatus, out var spoiledCount);

        Assert.False(changed);
        Assert.Equal(BallotStatus.Ok, newStatus);
        Assert.Equal(3, spoiledCount);
    }

    [Fact]
    public void HasDuplicates2_KeepStatusCode()
    {
        var votes = new List<BallotVoteInfo>
        {
            new() { PersonGuid = Guid.NewGuid() },
        };

        var model = new BallotAnalyzer(3, false);

        var changed = model.DetermineStatusFromVotes(BallotStatus.Review, votes, out var newStatus, out var spoiledCount);
        Assert.False(changed);
        Assert.Equal(BallotStatus.Review, newStatus);
        Assert.Equal(0, spoiledCount);

        changed = model.DetermineStatusFromVotes(BallotStatus.Ok, votes, out newStatus, out spoiledCount);
        Assert.True(changed);
        Assert.Equal(BallotStatus.TooFew, newStatus);
        Assert.Equal(0, spoiledCount);
    }

    [Fact]
    public void DetermineVoteStatus_Raw()
    {
        var vote = new BallotVoteInfo
        {
            OnlineVoteRaw = "some raw data",
            PersonGuid = null,
            PersonIneligibleReasonGuid = null,
            VoteIneligibleReasonCode = null
        };

        var status = BallotAnalyzer.DetermineVoteStatus(vote);
        Assert.Equal(VoteStatus.Raw, status);
    }

    [Fact]
    public void DetermineVoteStatus_Spoiled_CannotReceiveVotes()
    {
        var vote = new BallotVoteInfo
        {
            PersonGuid = Guid.NewGuid(),
            PersonCanReceiveVotes = false
        };

        var status = BallotAnalyzer.DetermineVoteStatus(vote);
        Assert.Equal(VoteStatus.Spoiled, status);
    }

    [Fact]
    public void DetermineVoteStatus_Changed()
    {
        var vote = new BallotVoteInfo
        {
            PersonGuid = Guid.NewGuid(),
            PersonCanReceiveVotes = true,
            PersonCombinedInfo = "new name info",
            VoteCombinedInfo = "old name info"
        };

        var status = BallotAnalyzer.DetermineVoteStatus(vote);
        Assert.Equal(VoteStatus.Changed, status);
    }

    [Fact]
    public void DetermineVoteStatus_Ok()
    {
        var vote = new BallotVoteInfo
        {
            PersonGuid = Guid.NewGuid(),
            PersonCanReceiveVotes = true,
            PersonCombinedInfo = "abc",
            VoteCombinedInfo = "abc"
        };

        var status = BallotAnalyzer.DetermineVoteStatus(vote);
        Assert.Equal(VoteStatus.Ok, status);
    }

    [Fact]
    public void BallotNeedsReview_ReviewStatus()
    {
        Assert.True(BallotAnalyzer.BallotNeedsReview(BallotStatus.Review));
        Assert.True(BallotAnalyzer.BallotNeedsReview(BallotStatus.Raw));
        Assert.True(BallotAnalyzer.BallotNeedsReview(BallotStatus.Verify));
        Assert.False(BallotAnalyzer.BallotNeedsReview(BallotStatus.Ok));
        Assert.False(BallotAnalyzer.BallotNeedsReview(BallotStatus.TooMany));
        Assert.False(BallotAnalyzer.BallotNeedsReview(BallotStatus.TooFew));
        Assert.False(BallotAnalyzer.BallotNeedsReview(BallotStatus.Dup));
        Assert.False(BallotAnalyzer.BallotNeedsReview(BallotStatus.Empty));
    }
}
