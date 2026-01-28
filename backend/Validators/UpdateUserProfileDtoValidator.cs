using FluentValidation;
using TallyJ4.DTOs.Account;

namespace TallyJ4.Validators;

/// <summary>
/// Validator for UpdateUserProfileDto that enforces user profile update requirements.
/// Validates username, email, and phone number formats and lengths.
/// </summary>
public class UpdateUserProfileDtoValidator : AbstractValidator<UpdateUserProfileDto>
{
    /// <summary>
    /// Initializes a new instance of the UpdateUserProfileDtoValidator with validation rules.
    /// </summary>
    public UpdateUserProfileDtoValidator()
    {
        RuleFor(x => x.UserName)
            .MaximumLength(256)
            .WithMessage("Username cannot exceed 256 characters")
            .Matches(@"^[a-zA-Z0-9_\-\.@]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.UserName))
            .WithMessage("Username can only contain letters, numbers, and characters: _ - . @");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Invalid email format")
            .MaximumLength(256)
            .WithMessage("Email cannot exceed 256 characters");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50)
            .WithMessage("Phone number cannot exceed 50 characters")
            .Matches(@"^[\d\s\-\(\)\+\.]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Phone number can only contain digits, spaces, and characters: - ( ) + .");
    }
}
