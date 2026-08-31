using Backend.DTOs.Elections;
using FluentValidation;

namespace Backend.Validators;

/// <summary>
/// Optional name for a duplicate; when present it must fit Election.Name.
/// </summary>
public class DuplicateElectionDtoValidator : AbstractValidator<DuplicateElectionDto>
{
    public DuplicateElectionDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(150)
            .When(x => !string.IsNullOrWhiteSpace(x.Name))
            .WithMessage("Election name cannot exceed 150 characters");
    }
}
