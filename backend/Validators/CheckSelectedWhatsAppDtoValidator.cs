using Backend.DTOs.People;
using FluentValidation;

namespace Backend.Validators;

/// <summary>
/// Bounds a check-selected WhatsApp request so one call cannot cover an entire imported roll.
/// </summary>
public class CheckSelectedWhatsAppDtoValidator : AbstractValidator<CheckSelectedWhatsAppDto>
{
    public CheckSelectedWhatsAppDtoValidator()
    {
        RuleFor(x => x.PersonGuids)
            .NotNull()
            .NotEmpty()
            .Must(list => list.Count <= CheckSelectedWhatsAppDto.MaxSelectedPeople)
            .WithMessage(
                $"At most {CheckSelectedWhatsAppDto.MaxSelectedPeople} people can be checked at once.");
    }
}
