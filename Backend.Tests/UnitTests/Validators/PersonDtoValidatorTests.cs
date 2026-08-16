using Backend.DTOs.People;
using Backend.Enumerations;
using Backend.Validators;

namespace Backend.Tests.UnitTests.Validators;

public class PersonDtoValidatorTests
{
    [Fact]
    public void CreatePerson_UnknownCode_ReturnsOnlyInvalidCodeMessage()
    {
        var validator = new CreatePersonDtoValidator();
        var dto = new CreatePersonDto
        {
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            IneligibleReasonCode = "ZZ9"
        };

        var result = validator.Validate(dto);

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Invalid ineligibility reason code");
        Assert.DoesNotContain(result.Errors, e => e.ErrorMessage.Contains("Internal"));
    }

    [Fact]
    public void CreatePerson_InternalOnlyCode_ReturnsInternalMessage()
    {
        var validator = new CreatePersonDtoValidator();
        var dto = new CreatePersonDto
        {
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            IneligibleReasonCode = IneligibleReasonEnum.U01_Unidentifiable.Code
        };

        var result = validator.Validate(dto);

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Internal ineligibility reasons cannot be used for person creation");
        Assert.DoesNotContain(result.Errors, e => e.ErrorMessage == "Invalid ineligibility reason code");
    }

    [Fact]
    public void UpdatePerson_UnknownCode_ReturnsOnlyInvalidCodeMessage()
    {
        var validator = new UpdatePersonDtoValidator();
        var dto = new UpdatePersonDto
        {
            LastName = "Smith",
            IneligibleReasonCode = "ZZ9"
        };

        var result = validator.Validate(dto);

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Invalid ineligibility reason code");
        Assert.DoesNotContain(result.Errors, e => e.ErrorMessage.Contains("Internal"));
    }
}
