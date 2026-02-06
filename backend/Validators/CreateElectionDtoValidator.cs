using FluentValidation;
using TallyJ4.DTOs.Elections;

namespace TallyJ4.Validators;

/// <summary>
/// Validator for CreateElectionDto that enforces election creation requirements.
/// Validates election name, date, type, and related parameters.
/// </summary>
public class CreateElectionDtoValidator : AbstractValidator<CreateElectionDto>
{
    /// <summary>
    /// Initializes a new instance of the CreateElectionDtoValidator with validation rules.
    /// </summary>
    public CreateElectionDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Election name is required")
            .MaximumLength(150)
            .WithMessage("Election name cannot exceed 150 characters");

        RuleFor(x => x.DateOfElection)
            .NotNull()
            .WithMessage("Date of election is required");

        RuleFor(x => x.ElectionType)
            .MaximumLength(5)
            .WithMessage("Election type cannot exceed 5 characters")
            .Must(type => string.IsNullOrWhiteSpace(type) ||
                         new[] { "STV", "Cond", "Multi" }.Contains(type))
            .WithMessage("Election type must be one of: STV, Cond, Multi");

        RuleFor(x => x.NumberToElect)
            .GreaterThan(0)
            .When(x => x.NumberToElect.HasValue)
            .WithMessage("Number to elect must be greater than 0");

        RuleFor(x => x.NumberExtra)
            .GreaterThanOrEqualTo(0)
            .When(x => x.NumberExtra.HasValue)
            .WithMessage("Number extra must be greater than or equal to 0");

        RuleFor(x => x.Convenor)
            .MaximumLength(150)
            .WithMessage("Convenor name cannot exceed 150 characters");

        RuleFor(x => x.CanVote)
            .MaximumLength(1)
            .WithMessage("CanVote must be a single character (Y/N/?)")
            .Must(val => string.IsNullOrWhiteSpace(val) || new[] { "Y", "N", "?" }.Contains(val))
            .WithMessage("CanVote must be Y, N, or ?");

        RuleFor(x => x.CanReceive)
            .MaximumLength(1)
            .WithMessage("CanReceive must be a single character (Y/N/?)")
            .Must(val => string.IsNullOrWhiteSpace(val) || new[] { "Y", "N", "?" }.Contains(val))
            .WithMessage("CanReceive must be Y, N, or ?");

        RuleFor(x => x.ElectionPasscode)
            .MaximumLength(50)
            .WithMessage("Election passcode cannot exceed 50 characters");

        RuleFor(x => x.LinkedElectionKind)
            .MaximumLength(2)
            .WithMessage("Linked election kind cannot exceed 2 characters");

        RuleFor(x => x.ElectionMode)
            .MaximumLength(1)
            .WithMessage("Election mode must be a single character")
            .Must(mode => string.IsNullOrWhiteSpace(mode) || new[] { "N", "I" }.Contains(mode))
            .WithMessage("Election mode must be N (Normal) or I (International)");

        RuleFor(x => x.OnlineSelectionProcess)
            .MaximumLength(1)
            .WithMessage("Online selection process must be a single character");

        RuleFor(x => x.EmailFromAddress)
            .MaximumLength(250)
            .WithMessage("Email from address cannot exceed 250 characters")
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.EmailFromAddress))
            .WithMessage("Email from address must be a valid email");

        RuleFor(x => x.EmailFromName)
            .MaximumLength(100)
            .WithMessage("Email from name cannot exceed 100 characters");

        RuleFor(x => x.EmailSubject)
            .MaximumLength(250)
            .WithMessage("Email subject cannot exceed 250 characters");

        RuleFor(x => x.SmsText)
            .MaximumLength(500)
            .WithMessage("SMS text cannot exceed 500 characters");

        RuleFor(x => x.CustomMethods)
            .MaximumLength(50)
            .WithMessage("Custom methods cannot exceed 50 characters");

        RuleFor(x => x.VotingMethods)
            .MaximumLength(10)
            .WithMessage("Voting methods cannot exceed 10 characters");
    }
}
