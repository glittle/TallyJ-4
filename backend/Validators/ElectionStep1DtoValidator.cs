using FluentValidation;
using TallyJ4.DTOs.Setup;

namespace TallyJ4.Validators;

/// <summary>
/// Validator for ElectionStep1Dto that enforces first step election setup requirements.
/// Validates election name, reason, and date constraints.
/// </summary>
public class ElectionStep1DtoValidator : AbstractValidator<ElectionStep1Dto>
{
    /// <summary>
    /// Initializes a new instance of the ElectionStep1DtoValidator with validation rules.
    /// </summary>
    public ElectionStep1DtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Election name is required")
            .MaximumLength(150).WithMessage("Election name must not exceed 150 characters");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");

        RuleFor(x => x.DateOfElection)
            .Must(date => !date.HasValue || date.Value >= DateTime.Today.AddYears(-1))
            .WithMessage("Election date cannot be more than 1 year in the past");
    }
}
