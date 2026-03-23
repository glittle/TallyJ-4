using Backend.Domain.Enumerations;
using Backend.DTOs.Ballots;
using FluentValidation;

namespace Backend.Validators;

/// <summary>
/// Validator for UpdateBallotDto that enforces ballot update requirements.
/// Validates status code, and teller name lengths.
/// </summary>
public class UpdateBallotDtoValidator : AbstractValidator<UpdateBallotDto>
{
    /// <summary>
    /// Initializes a new instance of the UpdateBallotDtoValidator with validation rules.
    /// </summary>
    public UpdateBallotDtoValidator()
    {
        RuleFor(x => x.StatusCode)
            .IsInEnum()
            .WithMessage($"Status code must be one of: {string.Join(", ", Enum.GetNames<BallotStatus>())}");

        RuleFor(x => x.Teller1)
            .MaximumLength(25)
            .WithMessage("Teller1 cannot exceed 25 characters");

        RuleFor(x => x.Teller2)
            .MaximumLength(25)
            .WithMessage("Teller2 cannot exceed 25 characters");
    }
}



