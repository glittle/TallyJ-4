using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class OnlineBallotStatusTests
{
    [Theory]
    [InlineData(null, true, OnlineBallotStatus.Draft)]
    [InlineData(OnlineBallotStatus.Draft, true, OnlineBallotStatus.Draft)]
    [InlineData(OnlineBallotStatus.Draft, false, OnlineBallotStatus.Submitted)]
    [InlineData(OnlineBallotStatus.Submitted, true, OnlineBallotStatus.Submitted)]
    [InlineData(OnlineBallotStatus.Submitted, false, OnlineBallotStatus.Submitted)]
    public void StatusAfterWrite_NeverDemotesSubmitted(
        string? currentStatus,
        bool isDraft,
        string expected)
    {
        Assert.Equal(expected, OnlineBallotStatus.StatusAfterWrite(currentStatus, isDraft));
    }
}
