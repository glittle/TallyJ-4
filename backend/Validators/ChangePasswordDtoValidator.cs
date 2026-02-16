using FluentValidation;
using Backend.DTOs.Account;

namespace Backend.Validators;

/// <summary>
/// Validator for ChangePasswordDto that enforces password change requirements.
/// Validates current password presence, new password strength, and confirmation matching.
/// </summary>
public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    /// <summary>
    /// Initializes a new instance of the ChangePasswordDtoValidator with validation rules.
    /// </summary>
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]")
            .WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]")
            .WithMessage("Password must contain at least one digit")
            .Matches(@"[\!\@\#\$\%\^\&\*\(\)\_\+\-\=\[\]\{\}\;\:\'\<\>\,\.\?\/\\\|]")
            .WithMessage("Password must contain at least one special character");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Password confirmation is required")
            .Equal(x => x.NewPassword)
            .WithMessage("Passwords do not match");
    }
}



