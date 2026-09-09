using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class OnlineVotingWindowTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsCurrentlyOpen_FalseWhenOnlineVotingDisabled()
    {
        Assert.False(OnlineVotingWindow.IsCurrentlyOpen(
            useOnlineVoting: false,
            onlineWhenOpen: Now.AddHours(-1),
            onlineWhenClose: Now.AddHours(1),
            now: Now));
    }

    [Fact]
    public void IsCurrentlyOpen_TrueWhenEnabledAndWindowDatesNull()
    {
        Assert.True(OnlineVotingWindow.IsCurrentlyOpen(
            useOnlineVoting: true,
            onlineWhenOpen: null,
            onlineWhenClose: null,
            now: Now));
    }

    [Fact]
    public void IsCurrentlyOpen_FalseBeforeScheduledOpen()
    {
        Assert.False(OnlineVotingWindow.IsCurrentlyOpen(
            useOnlineVoting: true,
            onlineWhenOpen: Now.AddHours(1),
            onlineWhenClose: Now.AddHours(2),
            now: Now));
    }

    [Fact]
    public void IsCurrentlyOpen_FalseAtOrAfterClose()
    {
        Assert.False(OnlineVotingWindow.IsCurrentlyOpen(
            useOnlineVoting: true,
            onlineWhenOpen: Now.AddHours(-2),
            onlineWhenClose: Now,
            now: Now));
    }

    [Fact]
    public void IsCurrentlyOpen_TrueInsideScheduledWindow()
    {
        Assert.True(OnlineVotingWindow.IsCurrentlyOpen(
            useOnlineVoting: true,
            onlineWhenOpen: Now.AddHours(-1),
            onlineWhenClose: Now.AddHours(1),
            now: Now));
    }
}
