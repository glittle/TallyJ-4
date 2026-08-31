using Backend.DTOs.Ballots;
using Backend.Validators;

namespace Backend.Tests.UnitTests.Validators;

public class CreateBallotDtoValidatorTests
{
    private readonly CreateBallotDtoValidator _validator = new();

    [Fact]
    public void ValidDto_Passes()
    {
        var result = _validator.Validate(new CreateBallotDto
        {
            ElectionGuid = Guid.NewGuid(),
            LocationGuid = Guid.NewGuid(),
            ComputerCode = "A",
            Teller1 = "Alice"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyLocationGuid_Fails()
    {
        var result = _validator.Validate(new CreateBallotDto
        {
            ElectionGuid = Guid.NewGuid(),
            LocationGuid = Guid.Empty,
            ComputerCode = "A"
        });

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Location GUID is required");
    }
}
