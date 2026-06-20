using Backend.DTOs.Tellers;
using FluentValidation;

namespace Backend.Validators;

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
    }
}