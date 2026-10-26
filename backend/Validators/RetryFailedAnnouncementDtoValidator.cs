using Backend.DTOs.People;
using Backend.Helpers;
using FluentValidation;

namespace Backend.Validators;

/// <summary>
/// Retry is only email, SMS, or WhatsApp.
/// </summary>
public class RetryFailedAnnouncementDtoValidator : AbstractValidator<RetryFailedAnnouncementDto>
{
    public RetryFailedAnnouncementDtoValidator()
    {
        RuleFor(x => x.Channel)
            .Must(AnnouncementChannels.IsRetryable)
            .WithMessage("people.announcement.channelInvalid");
    }
}
