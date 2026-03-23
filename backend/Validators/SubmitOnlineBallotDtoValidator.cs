using Backend.DTOs.OnlineVoting;
using FluentValidation;

namespace Backend.Validators;

/// <summary>
/// Validator for <see cref="SubmitOnlineBallotDto"/>.
/// </summary>
public class SubmitOnlineBallotDtoValidator : AbstractValidator<SubmitOnlineBallotDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitOnlineBallotDtoValidator"/> class.
    /// </summary>
    public SubmitOnlineBallotDtoValidator()
    {
        RuleFor(x => x.ElectionGuid)
            .NotEmpty()
            .WithMessage("Election GUID is required");

        RuleFor(x => x.VoterId)
            .NotEmpty()
            .WithMessage("Voter ID is required")
            .MaximumLength(250)
            .WithMessage("Voter ID cannot exceed 250 characters");

        RuleFor(x => x.Votes)
            .NotEmpty()
            .WithMessage("At least one vote is required");

        RuleForEach(x => x.Votes).ChildRules(vote =>
        {
            vote.RuleFor(v => v.PositionOnBallot)
                .GreaterThan(0)
                .WithMessage("Position on ballot must be greater than 0");
        });
    }
}



