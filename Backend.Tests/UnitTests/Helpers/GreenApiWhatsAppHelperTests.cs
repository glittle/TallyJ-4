using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class GreenApiWhatsAppHelperTests
{
    [Fact]
    public void IsConfigured_RejectsMissingAndPlaceholder()
    {
        Assert.False(GreenApiWhatsAppHelper.IsConfigured(null, "token"));
        Assert.False(GreenApiWhatsAppHelper.IsConfigured("1234", null));
        Assert.False(GreenApiWhatsAppHelper.IsConfigured("<GREEN-API-INSTANCE-ID>", "token"));
        Assert.False(GreenApiWhatsAppHelper.IsConfigured("1234", "<GREEN-API-TOKEN>"));
        Assert.True(GreenApiWhatsAppHelper.IsConfigured("1234", "token"));
    }

    [Fact]
    public void NormalizePhone_DigitsOnly()
    {
        Assert.Equal("14168972671", GreenApiWhatsAppHelper.NormalizePhone("+1 416-897-2671"));
    }

    [Fact]
    public void BuildCheckUrl_UsesCheckWhatsappPath()
    {
        Assert.Equal(
            "https://api.green-api.com/waInstance1234/checkWhatsapp/token",
            GreenApiWhatsAppHelper.BuildCheckUrl("https://api.green-api.com/", "1234", "token"));
    }

    [Fact]
    public void MapCheckResponse_ExistsTrue_IsOk()
    {
        Assert.Equal(
            OnlineVoterWhatsAppStatus.Ok,
            GreenApiWhatsAppHelper.MapCheckResponse("""{"existsWhatsapp":true}""", true));
    }

    [Fact]
    public void MapCheckResponse_ExistsFalse_IsNoWa()
    {
        Assert.Equal(
            OnlineVoterWhatsAppStatus.NoWa,
            GreenApiWhatsAppHelper.MapCheckResponse("""{"existsWhatsapp":false}""", true));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("{}", true)]
    [InlineData("""{"existsWhatsapp":true}""", false)]
    [InlineData("not-json", true)]
    public void MapCheckResponse_MissingOrFailed_IsCheckFailed(string? json, bool success)
    {
        Assert.Equal(
            OnlineVoterWhatsAppStatus.CheckFailed,
            GreenApiWhatsAppHelper.MapCheckResponse(json, success));
    }
}
