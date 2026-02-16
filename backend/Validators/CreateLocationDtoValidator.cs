using FluentValidation;
using Backend.DTOs.Locations;

namespace Backend.Validators;

/// <summary>
/// Validator for CreateLocationDto that enforces location creation requirements.
/// Validates location name, contact info, coordinates, and related parameters.
/// </summary>
public class CreateLocationDtoValidator : AbstractValidator<CreateLocationDto>
{
    /// <summary>
    /// Initializes a new instance of the CreateLocationDtoValidator with validation rules.
    /// </summary>
    public CreateLocationDtoValidator()
    {
        RuleFor(x => x.ElectionGuid)
            .NotEmpty()
            .WithMessage("Election GUID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Location name is required")
            .MaximumLength(50)
            .WithMessage("Location name cannot exceed 50 characters");

        RuleFor(x => x.ContactInfo)
            .MaximumLength(250)
            .WithMessage("Contact info cannot exceed 250 characters");

        RuleFor(x => x.Longitude)
            .MaximumLength(50)
            .WithMessage("Longitude cannot exceed 50 characters")
            .Matches(@"^-?([0-9]{1,3}(\.[0-9]+)?|180(\.0+)?)$")
            .When(x => !string.IsNullOrWhiteSpace(x.Longitude))
            .WithMessage("Longitude must be a valid coordinate between -180 and 180");

        RuleFor(x => x.Latitude)
            .MaximumLength(50)
            .WithMessage("Latitude cannot exceed 50 characters")
            .Matches(@"^-?([0-9]{1,2}(\.[0-9]+)?|90(\.0+)?)$")
            .When(x => !string.IsNullOrWhiteSpace(x.Latitude))
            .WithMessage("Latitude must be a valid coordinate between -90 and 90");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SortOrder.HasValue)
            .WithMessage("Sort order must be greater than or equal to 0");
    }
}



