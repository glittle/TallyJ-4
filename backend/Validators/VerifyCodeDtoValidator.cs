using FluentValidation;
using TallyJ4.DTOs.OnlineVoting;

namespace TallyJ4.Validators;

public class VerifyCodeDtoValidator : AbstractValidator<VerifyCodeDto>
{
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
