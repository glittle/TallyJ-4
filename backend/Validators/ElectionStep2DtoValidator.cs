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
            .IsInEnum()
            .WithMessage($"Election type must be one of: {string.Join(", ", ElectionTypeEnum.AllCodes)}");

        RuleFor(x => x.ElectionMode)
            .IsInEnum()
            .WithMessage($"Election mode must be one of: {string.Join(", ", ElectionModeEnum.All.Select(m => $"{m.Code} ({m.Description})"))}");
    }
}
