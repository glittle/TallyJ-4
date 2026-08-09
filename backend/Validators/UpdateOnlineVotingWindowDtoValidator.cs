using Backend.DTOs.Elections;
using FluentValidation;

namespace Backend.Validators;

/// <summary>
/// Validator for online voting window updates (open/close only).
/// </summary>
public class UpdateOnlineVotingWindowDtoValidator : AbstractValidator<UpdateOnlineVotingWindowDto>
{
    public UpdateOnlineVotingWindowDtoValidator()
    {
        RuleFor(x => x)
            .Must(x =>
                !x.OnlineWhenOpen.HasValue
                || !x.OnlineWhenClose.HasValue
                || x.OnlineWhenOpen < x.OnlineWhenClose)
            .WithMessage("Online voting open time must be before close time");
    }
}
