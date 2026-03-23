using Backend.DTOs.OnlineVoting;
using FluentValidation;

namespace Backend.Validators;

/// <summary>
/// Validator for <see cref="VerifyCodeDto"/>.
/// </summary>
public class VerifyCodeDtoValidator : AbstractValidator<VerifyCodeDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyCodeDtoValidator"/> class.
    /// </summary>
    public VerifyCodeDtoValidator()
    {
        RuleFor(x => x.VoterId)
            .NotEmpty()
            .WithMessage("Voter ID is required")
            .MaximumLength(250)
            .WithMessage("Voter ID cannot exceed 250 characters");

        RuleFor(x => x.VerifyCode)
            .NotEmpty()
            .WithMessage("Verification code is required")
            .MaximumLength(15)
            .WithMessage("Verification code cannot exceed 15 characters");
    }
}



