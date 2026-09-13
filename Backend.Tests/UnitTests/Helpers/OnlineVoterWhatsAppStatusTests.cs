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

    [Fact]
    public void AllowsNotify_OnlyExactOk()
    {
        Assert.True(OnlineVoterWhatsAppStatus.AllowsNotify("OK"));
        Assert.False(OnlineVoterWhatsAppStatus.AllowsNotify(null));
        Assert.False(OnlineVoterWhatsAppStatus.AllowsNotify("no-wa"));
        Assert.False(OnlineVoterWhatsAppStatus.AllowsNotify("check-failed"));
        Assert.False(OnlineVoterWhatsAppStatus.AllowsNotify("ok"));
    }
}
