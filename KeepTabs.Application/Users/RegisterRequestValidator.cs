using FluentValidation;
using KeepTabs.Application.Users.Dtos;

namespace KeepTabs.Application.Users;

/// <summary>
/// Shape validation for registration requests.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be valid.")
            .MaximumLength(256);

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(256);

        When(request => request.FirstName is not null, () =>
        {
            RuleFor(request => request.FirstName!)
                .MaximumLength(100);
        });

        When(request => request.LastName is not null, () =>
        {
            RuleFor(request => request.LastName!)
                .MaximumLength(100);
        });
    }
}
