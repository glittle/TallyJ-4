using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class SmsLogSendHelperTests
{
    [Fact]
    public void TryCreate_SidAndPhone_SetsDatesAndDefaultStatus()
    {
        var before = DateTimeOffset.UtcNow;

        var log = SmsLogSendHelper.TryCreate("SMabc", "+14168972671", lastStatus: null);

        Assert.NotNull(log);
        Assert.Equal("SMabc", log.SmsSid);
        Assert.Equal("+14168972671", log.Phone);
        Assert.Equal(SmsLogSendHelper.DefaultLastStatus, log.LastStatus);
        Assert.Null(log.ElectionGuid);
        Assert.Null(log.PersonGuid);
        Assert.True(log.SentDate >= before);
        Assert.Equal(log.SentDate, log.LastDate);
    }

    [Fact]
    public void TryCreate_ProviderStatus_Used()
    {
        var log = SmsLogSendHelper.TryCreate("SMabc", "+14168972671", "queued");

        Assert.NotNull(log);
        Assert.Equal("queued", log.LastStatus);
    }

    [Theory]
    [InlineData(null, "+14168972671")]
    [InlineData("", "+14168972671")]
    [InlineData("   ", "+14168972671")]
    [InlineData("SMabc", null)]
    [InlineData("SMabc", "")]
    [InlineData("SMabc", "   ")]
    public void TryCreate_MissingSidOrPhone_Null(string? sid, string? phone)
    {
        Assert.Null(SmsLogSendHelper.TryCreate(sid, phone, "queued"));
    }
}
