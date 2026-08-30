using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class TwilioRequestSignatureTests
{
    // Published Twilio RequestValidator example:
    // https://www.twilio.com/docs/usage/webhooks/webhooks-security
    private const string DocsAuthToken = "12345";
    private const string DocsUrl = "https://mycompany.com/myapp.php?foo=1&bar=2";
    private static readonly Dictionary<string, string> DocsForm = new()
    {
        ["CallSid"] = "CA1234567890ABCDE",
        ["Caller"] = "+14158675309",
        ["Digits"] = "1234",
        ["From"] = "+14158675309",
        ["To"] = "+18005551212"
    };

    [Fact]
    public void Compute_MatchesTwilioDocsVector()
    {
        var signature = TwilioRequestSignature.Compute(DocsAuthToken, DocsUrl, DocsForm);

        Assert.Equal("RSOYDt4T1cUTdK1PDd93/VVr8B8=", signature);
    }

    [Fact]
    public void IsValid_DocsVector_True()
    {
        Assert.True(TwilioRequestSignature.IsValid(
            DocsAuthToken, DocsUrl, DocsForm, "RSOYDt4T1cUTdK1PDd93/VVr8B8="));
    }

    [Fact]
    public void IsValid_WrongSignature_False()
    {
        Assert.False(TwilioRequestSignature.IsValid(
            DocsAuthToken, DocsUrl, DocsForm, "AAAAAAAAAAAAAAAAAAAAAAAAAAA="));
    }

    [Fact]
    public void IsValid_MissingSignature_False()
    {
        Assert.False(TwilioRequestSignature.IsValid(DocsAuthToken, DocsUrl, DocsForm, null));
        Assert.False(TwilioRequestSignature.IsValid(DocsAuthToken, DocsUrl, DocsForm, ""));
    }

    [Fact]
    public void IsAuthTokenConfigured_PlaceholderOrBlank_False()
    {
        Assert.False(TwilioRequestSignature.IsAuthTokenConfigured(null));
        Assert.False(TwilioRequestSignature.IsAuthTokenConfigured(""));
        Assert.False(TwilioRequestSignature.IsAuthTokenConfigured("<TWILIO-AUTH-TOKEN>"));
    }

    [Fact]
    public void IsAuthTokenConfigured_RealToken_True()
    {
        Assert.True(TwilioRequestSignature.IsAuthTokenConfigured("test-twilio-auth-token"));
    }
}
