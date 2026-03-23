using Backend.Domain.Enumerations;
using Backend.DTOs.People;
using FluentValidation;

namespace Backend.Validators;

/// <summary>
/// Validator for UpdatePersonDto that enforces person update requirements.
/// Validates names, contact information, and other person details.
/// </summary>
public class UpdatePersonDtoValidator : AbstractValidator<UpdatePersonDto>
{
    /// <summary>
    /// Initializes a new instance of the UpdatePersonDtoValidator with validation rules.
    /// </summary>
    public UpdatePersonDtoValidator()
    {
        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .MaximumLength(50)
            .WithMessage("Last name cannot exceed 50 characters");

        RuleFor(x => x.FirstName)
            .MaximumLength(50)
            .WithMessage("First name cannot exceed 50 characters");

        RuleFor(x => x.OtherLastNames)
            .MaximumLength(100)
            .WithMessage("Other last names cannot exceed 100 characters");

        RuleFor(x => x.OtherNames)
            .MaximumLength(100)
            .WithMessage("Other names cannot exceed 100 characters");

        RuleFor(x => x.OtherInfo)
            .MaximumLength(150)
            .WithMessage("Other info cannot exceed 150 characters");

        RuleFor(x => x.Area)
            .MaximumLength(50)
            .WithMessage("Area cannot exceed 50 characters");

        RuleFor(x => x.BahaiId)
            .MaximumLength(20)
            .WithMessage("Bahai ID cannot exceed 20 characters");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Invalid email format")
            .MaximumLength(250)
            .WithMessage("Email cannot exceed 250 characters");

        RuleFor(x => x.Phone)
            .MaximumLength(25)
            .WithMessage("Phone cannot exceed 25 characters")
            .Matches(@"^[\d\s\-\(\)\+\.]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Phone can only contain digits, spaces, and characters: - ( ) + .");

        RuleFor(x => x.AgeGroup)
            .MaximumLength(2)
            .WithMessage("Age group cannot exceed 2 characters");

        RuleFor(x => x.IneligibleReasonGuid)
            .Must(guid => guid == null || IneligibleReasonEnum.GetByGuid(guid) != null)
            .WithMessage("Invalid ineligibility reason GUID")
            .Must(guid =>
            {
                if (guid == null)
                {
                    return true;
                }

                var reason = IneligibleReasonEnum.GetByGuid(guid);
                return reason?.InternalOnly != true;
            })
            .WithMessage("Internal ineligibility reasons cannot be used for person updates");
    }
}



