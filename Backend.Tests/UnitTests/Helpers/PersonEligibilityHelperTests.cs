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
}
