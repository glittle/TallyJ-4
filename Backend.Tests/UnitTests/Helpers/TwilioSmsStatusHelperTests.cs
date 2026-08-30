using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class TwilioSmsStatusHelperTests
{
    [Theory]
    [InlineData("undelivered", 30003, "twilio-30003")]
    [InlineData("failed", 30003, "twilio-30003")]
    [InlineData("UNDELIVERED", 30005, "twilio-30005")]
    [InlineData("failed", 30006, "twilio-30006")]
    [InlineData("undelivered", 30004, "twilio-30004")]
    [InlineData("failed", 21211, "twilio-21211")]
    [InlineData("undelivered", 21614, "twilio-21614")]
    public void TryLearnReason_TerminalFailureWithSelectedCode_ReturnsTwilioReason(
        string status, int errorCode, string expected)
    {
        Assert.Equal(expected, TwilioSmsStatusHelper.TryLearnReason(status, errorCode));
    }

    [Theory]
    [InlineData("undelivered", null)]
    [InlineData("failed", null)]
    [InlineData("undelivered", 30007)]
    [InlineData("failed", 30008)]
    [InlineData("delivered", 30003)]
    [InlineData("sent", 30003)]
    [InlineData("queued", 30003)]
    [InlineData("sending", 30003)]
    [InlineData(null, 30003)]
    public void TryLearnReason_NonTerminalOrUnlisted_ReturnsNull(string? status, int? errorCode)
    {
        Assert.Null(TwilioSmsStatusHelper.TryLearnReason(status, errorCode));
    }

    [Fact]
    public void VoterIdLookupKeys_WithPlus_IncludesDigitsOnlyVariant()
    {
        var keys = TwilioSmsStatusHelper.VoterIdLookupKeys("+14168972671");
        Assert.Equal(["+14168972671", "14168972671"], keys);
    }

    [Fact]
    public void VoterIdLookupKeys_WithoutPlus_IncludesPlusVariant()
    {
        var keys = TwilioSmsStatusHelper.VoterIdLookupKeys("14168972671");
        Assert.Equal(["14168972671", "+14168972671"], keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void VoterIdLookupKeys_Blank_Empty(string? to)
    {
        Assert.Empty(TwilioSmsStatusHelper.VoterIdLookupKeys(to));
    }
}
