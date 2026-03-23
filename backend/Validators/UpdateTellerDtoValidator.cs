using Backend.DTOs.Tellers;
using FluentValidation;

namespace Backend.Validators;

/// <summary>
/// Validator for <see cref="UpdateTellerDto"/>.
/// </summary>
public class UpdateTellerDtoValidator : AbstractValidator<UpdateTellerDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTellerDtoValidator"/> class.
    /// </summary>
    public UpdateTellerDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Teller name is required")
            .MaximumLength(50)
            .WithMessage("Teller name cannot exceed 50 characters");

        RuleFor(x => x.UsingComputerCode)
            .MaximumLength(2)
            .WithMessage("Computer code must be exactly 2 characters")
            .Matches(@"^[A-Z]{2}$")
            .When(x => !string.IsNullOrWhiteSpace(x.UsingComputerCode))
            .WithMessage("Computer code must be 2 uppercase letters (AA-ZZ)");
    }
}



