using Backend.Services;
using Xunit;

namespace Backend.Tests.UnitTests.Services;

public class GuestCloseoutPolicyTests
{
    [Theory]
    [InlineData(1, 0, GuestCloseoutAction.StartTimer)]
    [InlineData(2, 0, GuestCloseoutAction.StartTimer)]
    [InlineData(0, 1, GuestCloseoutAction.CancelTimer)]
    [InlineData(0, 2, GuestCloseoutAction.CancelTimer)]
    [InlineData(0, 0, GuestCloseoutAction.None)]
    [InlineData(2, 1, GuestCloseoutAction.None)]
    [InlineData(1, 2, GuestCloseoutAction.None)]
    [InlineData(3, 2, GuestCloseoutAction.None)]
    public void Decide_returns_expected_action(
        int previousMainCount,
        int newMainCount,
        GuestCloseoutAction expected)
    {
        Assert.Equal(expected, GuestCloseoutPolicy.Decide(previousMainCount, newMainCount));
    }

    /// <summary>
    /// AC5 sequences from the verification plan: main leave, main rejoin, guest assign/release churn.
    /// </summary>
    [Fact]
    public void Decide_ac5_sequence_transitions()
    {
        var mainCount = 1;

        // (a) main leave → Start
        var afterMainLeave = 0;
        Assert.Equal(
            GuestCloseoutAction.StartTimer,
            GuestCloseoutPolicy.Decide(mainCount, afterMainLeave));
        mainCount = afterMainLeave;

        // (b) main rejoin → Cancel
        var afterMainRejoin = 1;
        Assert.Equal(
            GuestCloseoutAction.CancelTimer,
            GuestCloseoutPolicy.Decide(mainCount, afterMainRejoin));
        mainCount = afterMainRejoin;

        // Reset to post-main-leave state for guest-only paths
        mainCount = 0;

        // (c) guest Assign while mains already 0 → None
        Assert.Equal(GuestCloseoutAction.None, GuestCloseoutPolicy.Decide(mainCount, mainCount));

        // (d) guest Release while mains still 0 → None
        Assert.Equal(GuestCloseoutAction.None, GuestCloseoutPolicy.Decide(mainCount, mainCount));

        // (e) guest Release then guest Assign reconnect → None / None
        Assert.Equal(GuestCloseoutAction.None, GuestCloseoutPolicy.Decide(mainCount, mainCount));
        Assert.Equal(GuestCloseoutAction.None, GuestCloseoutPolicy.Decide(mainCount, mainCount));
    }

    [Theory]
    [InlineData(0, 1, true)]
    [InlineData(0, 2, true)]
    [InlineData(1, 0, true)]
    [InlineData(2, 0, true)]
    [InlineData(1, 2, false)]
    [InlineData(2, 1, false)]
    [InlineData(0, 0, false)]
    public void ShouldRefreshGuestJoinList_matches_main_teller_boundary_transitions(
        int previousMainCount,
        int newMainCount,
        bool expected)
    {
        Assert.Equal(
            expected,
            GuestCloseoutPolicy.ShouldRefreshGuestJoinList(previousMainCount, newMainCount));
    }
}