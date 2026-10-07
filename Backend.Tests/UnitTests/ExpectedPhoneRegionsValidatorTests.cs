using Backend.Configuration;
using Backend.DTOs.Elections;
using Backend.Helpers;
using Backend.Validators;
using Microsoft.Extensions.Options;

namespace Backend.Tests.UnitTests;

/// <summary>
/// Expected phone countries must be libphonenumber region codes and fit the column.
/// </summary>
public class ExpectedPhoneRegionsValidatorTests
{
    private readonly CreateElectionDtoValidator _validator = new(
        Options.Create(new TellerLoginProtectionOptions()));

    [Fact]
    public async Task CaAndUs_AreAccepted()
    {
        var result = await _validator.ValidateAsync(new CreateElectionDto
        {
            Name = "Regions",
            ExpectedPhoneRegions = "CA, US"
        });

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(CreateElectionDto.ExpectedPhoneRegions));
    }

    [Fact]
    public async Task Usa_IsRejected()
    {
        var result = await _validator.ValidateAsync(new CreateElectionDto
        {
            Name = "Regions",
            ExpectedPhoneRegions = "USA"
        });

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(CreateElectionDto.ExpectedPhoneRegions)
                && error.ErrorMessage == ExpectedPhoneRegions.InvalidMessageKey);
    }

    [Fact]
    public async Task OverEightyCharacters_IsRejected()
    {
        var result = await _validator.ValidateAsync(new CreateElectionDto
        {
            Name = "Regions",
            ExpectedPhoneRegions = string.Concat(Enumerable.Repeat("CA,", 27))
        });

        Assert.Equal(81, string.Concat(Enumerable.Repeat("CA,", 27)).Length);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(CreateElectionDto.ExpectedPhoneRegions)
                && error.ErrorMessage == ExpectedPhoneRegions.TooLongMessageKey);
    }
}
