using Backend.DTOs.People;
using FluentValidation;

namespace Backend.Validators;

/// <summary>
/// Bounds a WhatsApp notify start so one call cannot cover an entire imported roll.
/// </summary>
public class StartWhatsAppNotifyDtoValidator : AbstractValidator<StartWhatsAppNotifyDto>
{
    public StartWhatsAppNotifyDtoValidator()
    {
        RuleFor(x => x.PersonGuids)
            .NotNull()
            .NotEmpty()
            .Must(list => list.Count <= StartWhatsAppNotifyDto.MaxSelectedPeople)
            .WithMessage(
                $"At most {StartWhatsAppNotifyDto.MaxSelectedPeople} people can be queued at once.");
    }
}
