using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class OnlineBallotAcceptLockTests
{
    [Fact]
    public void TryEnter_SecondCallForSameElection_FailsUntilExit()
    {
        var gate = new OnlineBallotAcceptLock();
        var electionGuid = Guid.NewGuid();

        Assert.True(gate.TryEnter(electionGuid));
        Assert.False(gate.TryEnter(electionGuid));
        Assert.True(gate.TryEnter(Guid.NewGuid()));

        gate.Exit(electionGuid);
        Assert.True(gate.TryEnter(electionGuid));
    }
}
