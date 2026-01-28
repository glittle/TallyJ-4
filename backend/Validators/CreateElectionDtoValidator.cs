using FluentValidation;
using TallyJ4.DTOs.Elections;

namespace TallyJ4.Validators;

/// <summary>
/// Validator for CreateElectionDto that enforces election creation requirements.
/// Validates election name, date, type, and related parameters.
/// </summary>
public class CreateElectionDtoValidator : AbstractValidator<CreateElectionDto>
{
    /// <summary>
    /// Initializes a new instance of the CreateElectionDtoValidator with validation rules.
    /// </summary>
    public CreateElectionDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Election name is required")
            .MaximumLength(150)
            .WithMessage("Election name cannot exceed 150 characters");

        RuleFor(x => x.DateOfElection)
            .NotNull()
            .WithMessage("Date of election is required");

        RuleFor(x => x.ElectionType)
            .MaximumLength(5)
            .WithMessage("Election type cannot exceed 5 characters")
            .Must(type => string.IsNullOrWhiteSpace(type) || 
                         new[] { "STV", "Cond", "Multi" }.Contains(type))
            .WithMessage("Election type must be one of: STV, Cond, Multi");

        RuleFor(x => x.NumberToElect)
            .GreaterThan(0)
            .When(x => x.NumberToElect.HasValue)
            .WithMessage("Number to elect must be greater than 0");

        RuleFor(x => x.NumberExtra)
            .GreaterThanOrEqualTo(0)
            .When(x => x.NumberExtra.HasValue)
            .WithMessage("Number extra must be greater than or equal to 0");

        RuleFor(x => x.Convenor)
            .MaximumLength(150)
            .WithMessage("Convenor name cannot exceed 150 characters");
    }
}
