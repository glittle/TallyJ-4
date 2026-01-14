using FluentValidation;
using TallyJ4.DTOs.Elections;

namespace TallyJ4.Validators;

public class UpdateElectionDtoValidator : AbstractValidator<UpdateElectionDto>
{
    public UpdateElectionDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Election name is required")
            .MaximumLength(150)
            .WithMessage("Election name cannot exceed 150 characters");

        RuleFor(x => x.DateOfElection)
            .NotNull()
            .WithMessage("Date of election is required");

        RuleFor(x => x.NumberToElect)
            .GreaterThan(0)
            .When(x => x.NumberToElect.HasValue)
            .WithMessage("Number to elect must be greater than 0");

        RuleFor(x => x.NumberExtra)
            .GreaterThanOrEqualTo(0)
            .When(x => x.NumberExtra.HasValue)
            .WithMessage("Number extra must be greater than or equal to 0");

        RuleFor(x => x.TallyStatus)
            .MaximumLength(15)
            .WithMessage("Tally status cannot exceed 15 characters")
            .Must(status => string.IsNullOrWhiteSpace(status) || 
                           new[] { "Setup", "Ready", "Processing", "Finalized" }.Contains(status))
            .WithMessage("Tally status must be one of: Setup, Ready, Processing, Finalized");

        RuleFor(x => x.Convenor)
            .MaximumLength(150)
            .WithMessage("Convenor name cannot exceed 150 characters");
    }
}
