using FluentValidation;
using Backend.DTOs.Ballots;

namespace Backend.Validators;

/// <summary>
/// Validator for CreateBallotDto that enforces ballot creation requirements.
/// Validates election GUID presence and computer code format.
/// </summary>
public class CreateBallotDtoValidator : AbstractValidator<CreateBallotDto>
{
    /// <summary>
    /// Initializes a new instance of the CreateBallotDtoValidator with validation rules.
    /// </summary>
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



