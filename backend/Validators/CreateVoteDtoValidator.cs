using Backend.DTOs.Votes;
using FluentValidation;

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

        RuleFor(x => x)
            .Must(dto => dto.PersonGuid.HasValue || IsPersonLessVoteReason(dto.IneligibleReasonCode))
            .WithMessage("Either a person must be specified, or the ineligible reason must be U01 or U02");

        RuleFor(x => x.IneligibleReasonCode)
            .Must(code => IsPersonLessVoteReason(code))
            .When(x => !x.PersonGuid.HasValue)
            .WithMessage("Ineligible reason must be U01 or U02 when no person is specified");

        RuleFor(x => x.IneligibleReasonCode)
            .Empty()
            .When(x => x.PersonGuid.HasValue)
            .WithMessage("Ineligible reason cannot be set when a person is specified");
    }

    private static bool IsPersonLessVoteReason(string? code) =>
        code is "U01" or "U02";
}



