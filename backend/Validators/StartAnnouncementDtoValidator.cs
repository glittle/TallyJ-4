using Backend.DTOs.People;
using Backend.Helpers;
using FluentValidation;

namespace Backend.Validators;

/// <summary>
/// Bounds an email or SMS announcement start.
/// </summary>
public class StartAnnouncementDtoValidator : AbstractValidator<StartAnnouncementDto>
{
    public StartAnnouncementDtoValidator()
    {
        RuleFor(x => x.Channel)
            .Must(AnnouncementChannels.IsDirect)
            .WithMessage("people.announcement.channelInvalid");

        RuleFor(x => x.PersonGuids)
            .NotNull()
            .NotEmpty()
            .Must(list => list.Count <= StartAnnouncementDto.MaxSelectedPeople)
            .WithMessage("people.announcement.tooMany");
    }
}
