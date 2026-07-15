using Backend.DTOs.Account;
using FluentValidation;

namespace Backend.Validators;

public class ChangeDisplayNameDtoValidator : AbstractValidator<ChangeDisplayNameDto>
{
    public ChangeDisplayNameDtoValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Display name cannot be empty or whitespace only")
            .MaximumLength(200)
            .WithMessage("Display name cannot exceed 200 characters");
    }
}
