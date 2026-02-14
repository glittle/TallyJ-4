using FluentValidation;
using TallyJ4.Domain.Enumerations;
using TallyJ4.DTOs.Setup;

namespace TallyJ4.Validators;

/// <summary>
/// Validator for ElectionStep2Dto that enforces second step election setup requirements.
/// Validates election GUID, number to elect, election type, and election mode.
/// </summary>
public class ElectionStep2DtoValidator : AbstractValidator<ElectionStep2Dto>
{
    /// <summary>
    /// Initializes a new instance of the ElectionStep2DtoValidator with validation rules.
    /// </summary>
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
            .Must(type => ElectionTypeEnum.AllCodes.Contains(type))
            .WithMessage($"Election type must be one of: {string.Join(", ", ElectionTypeEnum.AllCodes)}");

        RuleFor(x => x.ElectionMode)
            .NotEmpty().WithMessage("Election mode is required")
            .MaximumLength(1).WithMessage("Election mode must be a single character")
            .Must(mode => ElectionModeEnum.AllCodes.Contains(mode))
            .WithMessage($"Election mode must be one of: {string.Join(", ", ElectionModeEnum.All.Select(m => $"{m.Code} ({m.Description})"))}");
    }
}
