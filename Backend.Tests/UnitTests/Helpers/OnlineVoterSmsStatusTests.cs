using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class OnlineVoterSmsStatusTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("OK")]
    public void AllowsPaidSend_NullOrOk(string? smsStatus)
    {
        Assert.True(OnlineVoterSmsStatus.AllowsPaidSend(smsStatus));
    }

    [Theory]
    [InlineData("undeliverable")]
    [InlineData("555-range")]
    [InlineData("landline")]
    [InlineData("premium")]
    [InlineData("admin")]
    [InlineData("twilio-30003")]
    [InlineData("ok")]
    [InlineData("")]
    public void AllowsPaidSend_AnyOtherValue_Blocks(string smsStatus)
    {
        Assert.False(OnlineVoterSmsStatus.AllowsPaidSend(smsStatus));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("OK")]
    public void CanLearnFromCallback_NullOrOk(string? smsStatus)
    {
        Assert.True(OnlineVoterSmsStatus.CanLearnFromCallback(smsStatus));
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("twilio-30003")]
    [InlineData("undeliverable")]
    public void CanLearnFromCallback_ExistingBlock_False(string smsStatus)
    {
        Assert.False(OnlineVoterSmsStatus.CanLearnFromCallback(smsStatus));
    }

    [Fact]
    public void TwilioReason_FormatsTwilioCode()
    {
        Assert.Equal("twilio-30003", OnlineVoterSmsStatus.TwilioReason(30003));
    }

    [Theory]
    [InlineData("OK", "OK")]
    [InlineData("ok", "OK")]
    [InlineData(" Ok ", "OK")]
    [InlineData("landline", "landline")]
    [InlineData(" admin ", "admin")]
    [InlineData("twilio-30003", "twilio-30003")]
    public void TryNormalizeManualValue_OkOrReason(string input, string expected)
    {
        Assert.True(OnlineVoterSmsStatus.TryNormalizeManualValue(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryNormalizeManualValue_Empty_False(string? input)
    {
        Assert.False(OnlineVoterSmsStatus.TryNormalizeManualValue(input, out var normalized));
        Assert.Null(normalized);
    }

    [Fact]
    public void TryNormalizeManualValue_Over50_False()
    {
        Assert.False(OnlineVoterSmsStatus.TryNormalizeManualValue(new string('x', 51), out _));
    }
}
