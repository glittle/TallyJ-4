using FluentValidation;
using TallyJ4.DTOs.Tellers;

namespace TallyJ4.Validators;

/// <summary>
/// Validator for <see cref="CreateTellerDto"/>.
/// </summary>
public class CreateTellerDtoValidator : AbstractValidator<CreateTellerDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTellerDtoValidator"/> class.
    /// </summary>
    public CreateTellerDtoValidator()
    {
        RuleFor(x => x.ElectionGuid)
            .NotEmpty()
            .WithMessage("Election GUID is required");

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
