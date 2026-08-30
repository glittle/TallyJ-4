using Backend.DTOs.OnlineVoting;
using Backend.Validators;

namespace Backend.Tests.UnitTests.Validators;

public class RequestCodeDtoValidatorTests
{
    private readonly RequestCodeDtoValidator _validator = new();

    [Fact]
    public void Phone_NormalE164_IsValid()
    {
        var result = _validator.Validate(PaidRequest("+14168972671"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("+15551234567")]
    [InlineData("+14155550100")]
    [InlineData("not-a-phone")]
    [InlineData("+123")]
    public void Phone_ReservedOrMalformed_IsInvalid(string phone)
    {
        var result = _validator.Validate(PaidRequest(phone));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RequestCodeDto.VoterId));
    }

    [Fact]
    public void Email_IsUnchangedByPhoneRules()
    {
        var result = _validator.Validate(new RequestCodeDto
        {
            VoterId = "voter@example.com",
            VoterIdType = "E",
            DeliveryMethod = "email"
        });

        Assert.True(result.IsValid);
    }

    private static RequestCodeDto PaidRequest(string phone) => new()
    {
        VoterId = phone,
        VoterIdType = "P",
        DeliveryMethod = "sms"
    };
}
