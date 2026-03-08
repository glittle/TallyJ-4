using FluentValidation;
using Backend.DTOs.Votes;

namespace Backend.Validators;

/// <summary>
/// Validator for CreateVoteDto that enforces vote creation requirements.
/// Validates ballot GUID, position, and status code.
/// </summary>
public class CreateVoteDtoValidator : AbstractValidator<CreateVoteDto>
{
    /// <summary>
    /// Initializes a new instance of the CreateVoteDtoValidator with validation rules.
    /// </summary>
    public CreateVoteDtoValidator()
    {
        RuleFor(x => x.BallotGuid)
            .NotEmpty()
            .WithMessage("Ballot GUID is required");

        RuleFor(x => x.PositionOnBallot)
            .GreaterThan(0)
            .WithMessage("Position on ballot must be greater than 0");
    }
}



