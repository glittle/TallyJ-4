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
    }
}