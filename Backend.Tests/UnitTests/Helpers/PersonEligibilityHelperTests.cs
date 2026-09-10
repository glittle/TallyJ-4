using Backend.Entities;
using Backend.Helpers;
using Backend.Enumerations;

namespace Backend.Tests.UnitTests.Helpers;

public class PersonEligibilityHelperTests
{
    [Fact]
    public void CanReceiveVotes_NullFlagsNoReason_IsTrue()
    {
        var person = new Person { LastName = "A", CanReceiveVotes = null, IneligibleReasonCode = null };
        Assert.True(PersonEligibilityHelper.CanReceiveVotes(person));
    }

    [Fact]
    public void CanReceiveVotes_ExplicitFalse_IsFalse()
    {
        var person = new Person { LastName = "A", CanReceiveVotes = false };
        Assert.False(PersonEligibilityHelper.CanReceiveVotes(person));
    }

    [Fact]
    public void CanReceiveVotes_NullWithCannotReceiveReason_IsFalse()
    {
        // Youth: can vote, cannot receive
        var reason = IneligibleReasonEnum.V01_YouthAged181920;
        var person = new Person
        {
            LastName = "A",
            CanReceiveVotes = null,
            IneligibleReasonCode = reason.Code
        };
        Assert.False(PersonEligibilityHelper.CanReceiveVotes(person));
    }

    [Fact]
    public void CanVote_NullFlagsNoReason_IsTrue()
    {
        var person = new Person { LastName = "A", CanVote = null, IneligibleReasonCode = null };
        Assert.True(PersonEligibilityHelper.CanVote(person));
    }

    [Fact]
    public void HasAcceptedBallot_VotingMethod_IsTrue()
    {
        var person = new Person { LastName = "A", VotingMethod = "P" };
        Assert.True(PersonEligibilityHelper.HasAcceptedBallot(person));
    }

    [Fact]
    public void HasAcceptedBallot_AcceptedOnlineBallot_IsTrue()
    {
        var person = new Person { LastName = "A", HasOnlineBallot = true };
        Assert.True(PersonEligibilityHelper.HasAcceptedBallot(person));
    }

    [Fact]
    public void HasAcceptedBallot_Neither_IsFalse()
    {
        var person = new Person { LastName = "A" };
        Assert.False(PersonEligibilityHelper.HasAcceptedBallot(person));
    }

    [Fact]
    public void ReasonRemovesVoteEligibility_XAndR_AreTrue_VAndEmpty_AreFalse()
    {
        Assert.True(PersonEligibilityHelper.ReasonRemovesVoteEligibility("X01"));
        Assert.True(PersonEligibilityHelper.ReasonRemovesVoteEligibility("R02"));
        Assert.True(PersonEligibilityHelper.ReasonRemovesVoteEligibility("ZZ9"));
        Assert.False(PersonEligibilityHelper.ReasonRemovesVoteEligibility("V01"));
        Assert.False(PersonEligibilityHelper.ReasonRemovesVoteEligibility(null));
        Assert.False(PersonEligibilityHelper.ReasonRemovesVoteEligibility(""));
    }
}
