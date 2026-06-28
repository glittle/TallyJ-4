using Backend.DTOs.Votes;
using FluentValidation;

namespace Backend.Validators;

public class ReorderVotesDtoValidator : AbstractValidator<ReorderVotesDto>
{
    public ReorderVotesDtoValidator()
    {
        RuleFor(x => x.BallotGuid)
            .NotEmpty()
            .WithMessage("Ballot GUID is required");

        RuleFor(x => x.VoteRowIds)
            .NotNull()
            .WithMessage("Vote row IDs are required")
            .Must(ids => ids.Count > 0)
            .WithMessage("At least one vote row ID is required")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Vote row IDs must be unique");
    }
}