using FluentValidation;
using TallyJ4.DTOs.Votes;

namespace TallyJ4.Validators;

public class CreateVoteDtoValidator : AbstractValidator<CreateVoteDto>
{
    public CreateVoteDtoValidator()
    {
        RuleFor(x => x.BallotGuid)
            .NotEmpty()
            .WithMessage("Ballot GUID is required");

        RuleFor(x => x.PositionOnBallot)
            .GreaterThan(0)
            .WithMessage("Position on ballot must be greater than 0");

        RuleFor(x => x.StatusCode)
            .NotEmpty()
            .WithMessage("Status code is required")
            .Must(BeValidStatusCode)
            .WithMessage("Status code must be one of: ok, Extra, Spoiled, Unreadable");
    }

    private bool BeValidStatusCode(string? statusCode)
    {
        if (string.IsNullOrEmpty(statusCode)) return false;

        var validCodes = new[] { "ok", "Extra", "Spoiled", "Unreadable" };
        return validCodes.Contains(statusCode);
    }
}
