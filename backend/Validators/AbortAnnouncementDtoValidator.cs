using Backend.DTOs.People;
using Backend.Helpers;
using FluentValidation;

namespace Backend.Validators;

/// <summary>
/// Abort is only the email or SMS queue. WhatsApp abort stays on its own endpoint.
/// </summary>
public class AbortAnnouncementDtoValidator : AbstractValidator<AbortAnnouncementDto>
{
    public AbortAnnouncementDtoValidator()
    {
        RuleFor(x => x.Channel)
            .Must(AnnouncementChannels.IsDirect)
            .WithMessage("people.announcement.channelInvalid");
    }
}
