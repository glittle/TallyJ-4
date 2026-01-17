using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TallyJ4.DTOs.Votes;
using TallyJ4.EF.Context;

namespace TallyJ4.Validators;

public class CreateVoteDtoValidator : AbstractValidator<CreateVoteDto>
{
    private readonly MainDbContext _context;

    public CreateVoteDtoValidator(MainDbContext context)
    {
        _context = context;

        RuleFor(x => x.BallotGuid)
            .NotEmpty()
            .WithMessage("Ballot GUID is required")
            .MustAsync(BallotExists)
            .WithMessage("Ballot does not exist");

        RuleFor(x => x.PositionOnBallot)
            .GreaterThan(0)
            .WithMessage("Position on ballot must be greater than 0");

        RuleFor(x => x.StatusCode)
            .NotEmpty()
            .WithMessage("Status code is required")
            .Must(BeValidStatusCode)
            .WithMessage("Status code must be one of: ok, Extra, Spoiled, Unreadable");

        When(x => x.PersonGuid.HasValue, () =>
        {
            RuleFor(x => x.PersonGuid!.Value)
                .MustAsync(PersonExists)
                .WithMessage("Person does not exist")
                .MustAsync(PersonIsEligible)
                .WithMessage("Person is not eligible to receive votes");

            RuleFor(x => x)
                .MustAsync(NoDuplicateVote)
                .WithMessage("This person already has a vote on this ballot");
        });
    }

    private async Task<bool> BallotExists(Guid ballotGuid, CancellationToken cancellationToken)
    {
        return await _context.Ballots.AnyAsync(b => b.BallotGuid == ballotGuid, cancellationToken);
    }

    private async Task<bool> PersonExists(Guid personGuid, CancellationToken cancellationToken)
    {
        return await _context.People.AnyAsync(p => p.PersonGuid == personGuid, cancellationToken);
    }

    private async Task<bool> PersonIsEligible(Guid personGuid, CancellationToken cancellationToken)
    {
        var person = await _context.People.FirstOrDefaultAsync(p => p.PersonGuid == personGuid, cancellationToken);
        return person?.CanReceiveVotes == true;
    }

    private async Task<bool> NoDuplicateVote(CreateVoteDto dto, CancellationToken cancellationToken)
    {
        if (!dto.PersonGuid.HasValue) return true;

        var exists = await _context.Votes
            .AnyAsync(v => v.BallotGuid == dto.BallotGuid && v.PersonGuid == dto.PersonGuid, cancellationToken);

        return !exists;
    }

    private bool BeValidStatusCode(string? statusCode)
    {
        if (string.IsNullOrEmpty(statusCode)) return false;

        var validCodes = new[] { "ok", "Extra", "Spoiled", "Unreadable" };
        return validCodes.Contains(statusCode);
    }
}
