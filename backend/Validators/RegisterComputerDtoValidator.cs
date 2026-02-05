using FluentValidation;
using TallyJ4.DTOs.Computers;

namespace TallyJ4.Validators;

/// <summary>
/// Validator for <see cref="RegisterComputerDto"/>.
/// </summary>
public class RegisterComputerDtoValidator : AbstractValidator<RegisterComputerDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterComputerDtoValidator"/> class.
    /// </summary>
    public RegisterComputerDtoValidator()
    {
        RuleFor(x => x.ElectionGuid)
            .NotEmpty()
            .WithMessage("Election GUID is required");

        RuleFor(x => x.LocationGuid)
            .NotEmpty()
            .WithMessage("Location GUID is required");

        RuleFor(x => x.ComputerCode)
            .Length(2)
            .When(x => !string.IsNullOrWhiteSpace(x.ComputerCode))
            .WithMessage("Computer code must be exactly 2 characters")
            .Matches(@"^[A-Z0-9]{2}$")
            .When(x => !string.IsNullOrWhiteSpace(x.ComputerCode))
            .WithMessage("Computer code must be 2 uppercase letters or numbers");

        RuleFor(x => x.BrowserInfo)
            .MaximumLength(250)
            .WithMessage("Browser info cannot exceed 250 characters");

        RuleFor(x => x.IpAddress)
            .MaximumLength(50)
            .WithMessage("IP address cannot exceed 50 characters");
    }
}
