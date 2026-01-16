using FluentValidation;
using TallyJ4.DTOs.Setup;

namespace TallyJ4.Validators;

public class ElectionStep2DtoValidator : AbstractValidator<ElectionStep2Dto>
{
    public ElectionStep2DtoValidator()
    {
        RuleFor(x => x.ElectionGuid)
            .NotEmpty().WithMessage("Election GUID is required");

        RuleFor(x => x.NumberToElect)
            .GreaterThan(0).WithMessage("Number to elect must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Number to elect must not exceed 100");

        RuleFor(x => x.ElectionType)
            .NotEmpty().WithMessage("Election type is required")
            .MaximumLength(5).WithMessage("Election type must not exceed 5 characters")
            .Must(type => new[] { "STV", "STVn" }.Contains(type))
            .WithMessage("Election type must be 'STV' or 'STVn'");

        RuleFor(x => x.ElectionMode)
            .NotEmpty().WithMessage("Election mode is required")
            .MaximumLength(1).WithMessage("Election mode must be a single character")
            .Must(mode => new[] { "N", "I" }.Contains(mode))
            .WithMessage("Election mode must be 'N' (Normal) or 'I' (International)");
    }
}
