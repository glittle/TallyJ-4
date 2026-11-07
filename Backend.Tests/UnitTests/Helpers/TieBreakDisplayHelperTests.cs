using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class TieBreakDisplayHelperTests
{
    [Fact]
    public void FormatVoteCountDisplay_Unset_OmitsSuffix()
    {
        Assert.Equal("50", TieBreakDisplayHelper.FormatVoteCountDisplay(50, null));
    }

    [Fact]
    public void FormatVoteCountDisplay_ExplicitZero_ShowsZero()
    {
        Assert.Equal("50 / 0", TieBreakDisplayHelper.FormatVoteCountDisplay(50, 0));
    }

    [Fact]
    public void FormatVoteCountDisplay_EnteredCount_ShowsCountAfterResolution()
    {
        Assert.Equal("50 / 3", TieBreakDisplayHelper.FormatVoteCountDisplay(50, 3));
    }
}
