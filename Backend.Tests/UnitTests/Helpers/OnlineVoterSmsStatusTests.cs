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
}
