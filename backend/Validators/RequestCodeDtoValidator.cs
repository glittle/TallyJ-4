using FluentValidation;
using Backend.DTOs.OnlineVoting;

namespace Backend.Validators;

/// <summary>
/// Validator for <see cref="RequestCodeDto"/>.
/// </summary>
public class RequestCodeDtoValidator : AbstractValidator<RequestCodeDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestCodeDtoValidator"/> class.
    /// </summary>
    public RequestCodeDtoValidator()
    {
        RuleFor(x => x.VoterId)
            .NotEmpty()
            .WithMessage("Voter ID is required")
            .MaximumLength(250)
            .WithMessage("Voter ID cannot exceed 250 characters");

        RuleFor(x => x.VoterIdType)
            .NotEmpty()
            .WithMessage("Voter ID type is required")
            .Must(x => x == "E" || x == "P" || x == "C")
            .WithMessage("Voter ID type must be 'E' (email), 'P' (phone), or 'C' (code)");

        RuleFor(x => x.DeliveryMethod)
            .NotEmpty()
            .WithMessage("Delivery method is required")
            .Must(x => x == "email" || x == "sms" || x == "voice")
            .WithMessage("Delivery method must be 'email', 'sms', or 'voice'");

        RuleFor(x => x.VoterId)
            .EmailAddress()
            .When(x => x.VoterIdType == "E")
            .WithMessage("Invalid email address");

        RuleFor(x => x.VoterId)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => x.VoterIdType == "P")
            .WithMessage("Invalid phone number format");
    }
}



