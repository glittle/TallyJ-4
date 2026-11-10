using System.ComponentModel.DataAnnotations;
using Backend.DTOs.Elections;
using FluentValidation;

namespace Backend.Validators;

/// <summary>
/// An election admin is added by the email of an existing officer account.
/// </summary>
public class AddElectionAdminDtoValidator : AbstractValidator<AddElectionAdminDto>
{
    public AddElectionAdminDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .Must(BeAnEmail)
            .WithMessage("A valid email is required")
            .MaximumLength(256);
    }

    private static bool BeAnEmail(string? email)
    {
        var trimmed = email?.Trim() ?? "";
        return trimmed.Length > 0 && new EmailAddressAttribute().IsValid(trimmed);
    }
}
