using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class AnnouncementErrorTextTests
{
    [Fact]
    public void Sanitize_KeepsATimeoutMessage()
    {
        Assert.Equal(
            "The operation has timed out.",
            AnnouncementErrorText.Sanitize("The operation has timed out."));
    }

    [Fact]
    public void Sanitize_StripsAddressesAndUrls()
    {
        var text = AnnouncementErrorText.Sanitize(
            "failed for ada@example.com number +14168972901 https://api.example.com/sendMessage/secret-token");

        Assert.NotNull(text);
        Assert.DoesNotContain("ada@example.com", text);
        Assert.DoesNotContain("14168972901", text);
        Assert.DoesNotContain("secret-token", text);
        Assert.DoesNotContain("http", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_KeepsAShortHttpStatus()
    {
        Assert.Equal("HTTP 500", AnnouncementErrorText.Sanitize("HTTP 500"));
    }
}
