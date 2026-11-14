using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class VotingMethodCodesTests
{
    [Theory]
    [InlineData(null, new[] { "P", "M", "D" })]
    [InlineData("", new[] { "P", "M", "D" })]
    [InlineData("PMD", new[] { "P", "M", "D" })]
    [InlineData("PDM", new[] { "P", "D", "M" })]
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

    [Theory]
    [InlineData("K")]
    [InlineData("KI")]
    [InlineData("OL")]
    [InlineData("C")]
    [InlineData("P")]
    [InlineData("IP")]
    [InlineData("IP,OL")]
    [InlineData("M")]
    [InlineData(null)]
    [InlineData("")]
    public void AllowsDeskMethod_PaperMethodsStayOnWhenStoredStringOmitsThem(string? stored)
    {
        Assert.True(VotingMethodCodes.AllowsDeskMethod(stored, "P"));
        Assert.True(VotingMethodCodes.AllowsDeskMethod(stored, "M"));
        Assert.True(VotingMethodCodes.AllowsDeskMethod(stored, "D"));
        Assert.True(VotingMethodCodes.AllowsDeskMethod(stored, "m"));
    }

    [Fact]
    public void AllowsDeskMethod_OtherMethodsStayConfigurable()
    {
        Assert.True(VotingMethodCodes.AllowsDeskMethod("K", "K"));
        Assert.True(VotingMethodCodes.AllowsDeskMethod("KI", "K"));
        Assert.False(VotingMethodCodes.AllowsDeskMethod("OL", "K"));
        Assert.False(VotingMethodCodes.AllowsDeskMethod("PMD", "C"));
        Assert.True(VotingMethodCodes.AllowsDeskMethod("PMD", "C", useCallInButton: true));
        Assert.True(VotingMethodCodes.AllowsDeskMethod("C", "C"));
        Assert.False(VotingMethodCodes.AllowsDeskMethod("PMDKO", "O"));
        Assert.False(VotingMethodCodes.AllowsDeskMethod("PMD", "I"));
        Assert.True(VotingMethodCodes.AllowsDeskMethod("IM", "I"));
        Assert.False(VotingMethodCodes.AllowsDeskMethod("K", "X"));
    }
}
