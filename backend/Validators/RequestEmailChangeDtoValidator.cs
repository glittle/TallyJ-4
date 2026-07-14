using Backend.DTOs.Account;
using FluentValidation;

namespace Backend.Validators;

public class RequestEmailChangeDtoValidator : AbstractValidator<RequestEmailChangeDto>
{
    public RequestEmailChangeDtoValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.CurrentPassword)
            .NotEmpty();
    }
}
