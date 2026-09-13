using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class WhatsAppNotifyMessageTests
{
    [Fact]
    public void Fill_ReplacesV3Placeholders()
    {
        var text = WhatsAppNotifyMessage.Fill(
            "Hi {FirstName} ({PersonName}) at {VoterContact} — {hostSite}",
            "Pat Smith",
            "Pat",
            "+14168972671",
            "https://example.test");

        Assert.Equal("Hi Pat (Pat Smith) at +14168972671 — https://example.test", text);
    }
}
