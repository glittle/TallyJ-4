using FluentValidation;
using TallyJ4.DTOs.Ballots;

namespace TallyJ4.Validators;

public class CreateBallotDtoValidator : AbstractValidator<CreateBallotDto>
{
    public CreateBallotDtoValidator()
    {
        RuleFor(x => x.ElectionGuid)
            .NotEmpty()
            .WithMessage("Election GUID is required");

        RuleFor(x => x.ComputerCode)
            .NotEmpty()
            .WithMessage("Computer code is required")
            .MaximumLength(2)
            .WithMessage("Computer code cannot exceed 2 characters")
            .Matches(@"^[A-Z]{1,2}$")
            .WithMessage("Computer code must be 1-2 uppercase letters");
    }
}
