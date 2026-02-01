using FluentValidation;
using TallyJ4.DTOs.OnlineVoting;

namespace TallyJ4.Validators;

public class SubmitOnlineBallotDtoValidator : AbstractValidator<SubmitOnlineBallotDto>
{
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
