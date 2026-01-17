using FluentValidation;
using TallyJ4.DTOs.Ballots;

namespace TallyJ4.Validators;

public class UpdateBallotDtoValidator : AbstractValidator<UpdateBallotDto>
{
    private static readonly string[] ValidStatusCodes = { "New", "Review", "OK", "Dup", "Spoil", "EmptyQ" };

    public UpdateBallotDtoValidator()
    {
        RuleFor(x => x.StatusCode)
            .NotEmpty()
            .WithMessage("Status code is required")
            .MaximumLength(10)
            .WithMessage("Status code cannot exceed 10 characters")
            .Must(code => ValidStatusCodes.Contains(code))
            .WithMessage($"Status code must be one of: {string.Join(", ", ValidStatusCodes)}");

        RuleFor(x => x.Teller1)
            .MaximumLength(25)
            .WithMessage("Teller1 cannot exceed 25 characters");

        RuleFor(x => x.Teller2)
            .MaximumLength(25)
            .WithMessage("Teller2 cannot exceed 25 characters");
    }
}
