using Backend.Helpers;
using Xunit;

namespace Backend.Tests.UnitTests.Helpers;

public class ElectionTellerAccessHelperTests
{
    [Fact]
    public void IsGuestTellerAccessOpen_null_is_closed()
    {
        Assert.False(ElectionTellerAccessHelper.IsGuestTellerAccessOpen(null));
    }

    [Fact]
    public void IsGuestTellerAccessOpen_future_date_is_closed()
    {
        var future = DateTimeOffset.UtcNow.AddHours(1);
        Assert.False(ElectionTellerAccessHelper.IsGuestTellerAccessOpen(future));
    }

    [Fact]
    public void IsGuestTellerAccessOpen_past_date_is_open()
    {
        var past = DateTimeOffset.UtcNow.AddMinutes(-5);
        Assert.True(ElectionTellerAccessHelper.IsGuestTellerAccessOpen(past));
    }
}