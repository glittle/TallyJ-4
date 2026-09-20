using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class TieBreakDisplayHelperTests
{
    [Fact]
    public void FormatVoteCountDisplay_NotRequired_OmitsSuffix()
    {
        Assert.Equal("50", TieBreakDisplayHelper.FormatVoteCountDisplay(50, false, null));
        Assert.Equal("50", TieBreakDisplayHelper.FormatVoteCountDisplay(50, false, 0));
        Assert.Equal("50", TieBreakDisplayHelper.FormatVoteCountDisplay(50, false, 3));
    }

    [Fact]
    public void FormatVoteCountDisplay_RequiredUnset_OmitsSuffix()
    {
        Assert.Equal("50", TieBreakDisplayHelper.FormatVoteCountDisplay(50, true, null));
    }

    [Fact]
    public void FormatVoteCountDisplay_RequiredExplicitZero_ShowsZero()
    {
        Assert.Equal("50 / 0", TieBreakDisplayHelper.FormatVoteCountDisplay(50, true, 0));
    }

    [Fact]
    public void FormatVoteCountDisplay_RequiredEntered_ShowsCount()
    {
        Assert.Equal("50 / 3", TieBreakDisplayHelper.FormatVoteCountDisplay(50, true, 3));
    }
}
