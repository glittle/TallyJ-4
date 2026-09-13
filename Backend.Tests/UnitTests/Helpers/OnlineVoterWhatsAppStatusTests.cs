using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class OnlineVoterWhatsAppStatusTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("OK")]
    public void AllowsSend_NullOrOk(string? whatsAppStatus)
    {
        Assert.True(OnlineVoterWhatsAppStatus.AllowsSend(whatsAppStatus));
    }

    [Theory]
    [InlineData("no-wa")]
    [InlineData("check-failed")]
    [InlineData("blocked")]
    [InlineData("ok")]
    [InlineData("")]
    public void AllowsSend_AnyOtherValue_Blocks(string whatsAppStatus)
    {
        Assert.False(OnlineVoterWhatsAppStatus.AllowsSend(whatsAppStatus));
    }
}
