using Backend.DTOs.People;
using Backend.Helpers;
using FluentValidation;

namespace Backend.Validators;

/// <summary>
/// Validates a manual teller SmsStatus set: required, max 50, not whitespace.
/// </summary>
public class SetPersonPhoneSmsStatusDtoValidator : AbstractValidator<SetPersonPhoneSmsStatusDto>
{
    public SetPersonPhoneSmsStatusDtoValidator()
    {
        RuleFor(x => x.SmsStatus)
            .Must(value => OnlineVoterSmsStatus.TryNormalizeManualValue(value, out _))
            .WithMessage("SMS status must be OK or a short block reason (max 50 characters).");
    }
}
