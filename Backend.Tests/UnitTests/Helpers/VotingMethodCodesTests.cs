using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class VotingMethodCodesTests
{
    [Theory]
    [InlineData(null, new[] { "P", "M", "D" })]
    [InlineData("", new[] { "P", "M", "D" })]
    [InlineData("PDM", new[] { "P", "M", "D" })]
    [InlineData("PMDOK", new[] { "P", "M", "D", "O", "K" })]
    [InlineData("IP,OL", new[] { "P", "O" })]
    [InlineData("K", new[] { "K" })]
    [InlineData("IM", new[] { "I" })]
    public void ParseElectionVotingMethods_MapsConcatenatedAndAliasedCodes(
        string? input,
        string[] expected)
    {
        Assert.Equal(expected, VotingMethodCodes.ParseElectionVotingMethods(input));
    }

    [Fact]
    public void Count_ProcessedOnlineWithoutMethod_IsOnline()
    {
        var breakdown = VotingMethodCodes.Count(
        [
            ("P", false),
            ("M", false),
            ("D", false),
            ("K", false),
            (null, true),
            ("O", true)
        ]);

        Assert.Equal(1, breakdown.InPerson);
        Assert.Equal(1, breakdown.Mailed);
        Assert.Equal(1, breakdown.DroppedOff);
        Assert.Equal(1, breakdown.Kiosk);
        Assert.Equal(2, breakdown.Online);
    }

    [Fact]
    public void Count_ProcessedPlusPaperMethod_DoesNotCountAsOnline()
    {
        var breakdown = VotingMethodCodes.Count([("P", true)]);

        Assert.Equal(1, breakdown.InPerson);
        Assert.Equal(0, breakdown.Online);
    }

    [Fact]
    public void ElectionSupportsKiosk_ReadsConcatenatedAndAliasedK()
    {
        Assert.True(VotingMethodCodes.ElectionSupportsKiosk("PMDK"));
        Assert.True(VotingMethodCodes.ElectionSupportsKiosk("IP,K"));
        Assert.False(VotingMethodCodes.ElectionSupportsKiosk("PDM"));
    }
}
