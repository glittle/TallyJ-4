using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class PaidDestinationPhoneTests
{
    [Theory]
    [InlineData("+14168972671")]
    [InlineData("14168972671")]
    [InlineData("+447911123456")]
    [InlineData("+16043871234")]
    public void IsAllowed_AcceptsNormalE164(string phone)
    {
        Assert.True(PaidDestinationPhone.IsAllowed(phone));
        Assert.True(PaidDestinationPhone.TryExplain(phone, out var reason));
        Assert.Null(reason);
    }

    [Theory]
    [InlineData("+15551234567")]
    [InlineData("15551234567")]
    [InlineData("5551234567")]
    [InlineData("+15550123456")]
    public void IsAllowed_RejectsNanpAreaCode555(string phone)
    {
        Assert.False(PaidDestinationPhone.IsAllowed(phone));
        Assert.False(PaidDestinationPhone.TryExplain(phone, out var reason));
        Assert.Equal(PaidDestinationPhone.ReasonNanp555, reason);
    }

    [Theory]
    [InlineData("+14155550100")]
    [InlineData("+14155551212")]
    [InlineData("14155550199")]
    [InlineData("4155550100")]
    public void IsAllowed_RejectsNanpExchange555(string phone)
    {
        Assert.False(PaidDestinationPhone.IsAllowed(phone));
        Assert.False(PaidDestinationPhone.TryExplain(phone, out var reason));
        Assert.Equal(PaidDestinationPhone.ReasonNanp555, reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-phone")]
    [InlineData("555-0100")]
    [InlineData("+0123456789")]
    [InlineData("+123")]
    [InlineData("+1")]
    [InlineData("1234567")]
    [InlineData("+1234567890123456")]
    public void IsAllowed_RejectsMalformedE164(string? phone)
    {
        Assert.False(PaidDestinationPhone.IsAllowed(phone));
        Assert.False(PaidDestinationPhone.TryExplain(phone, out var reason));
        Assert.Equal(PaidDestinationPhone.ReasonMalformedE164, reason);
    }

    [Theory]
    [InlineData("sms")]
    [InlineData("voice")]
    [InlineData("whatsapp")]
    public void IsPaidChannel_RecognizesPaidMethods(string method)
    {
        Assert.True(PaidDestinationPhone.IsPaidChannel(method));
    }

    [Theory]
    [InlineData("email")]
    [InlineData("")]
    [InlineData(null)]
    public void IsPaidChannel_IgnoresUnpaidMethods(string? method)
    {
        Assert.False(PaidDestinationPhone.IsPaidChannel(method));
    }
}
