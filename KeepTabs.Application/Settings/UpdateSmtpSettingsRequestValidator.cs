using FluentValidation;
using KeepTabs.Application.Settings.Dtos;

namespace KeepTabs.Application.Settings;

/// <summary>
/// Shape validation for SMTP configuration updates.
/// </summary>
public sealed class UpdateSmtpSettingsRequestValidator : AbstractValidator<UpdateSmtpSettingsRequest>
{
    public UpdateSmtpSettingsRequestValidator()
    {
        When(request => request.Enabled, () =>
        {
            RuleFor(request => request.Host)
                .NotEmpty()
                .WithMessage("SMTP host is required.")
                .MaximumLength(512);

            RuleFor(request => request.Port)
                .InclusiveBetween(1, 65_535)
                .WithMessage("SMTP port must be between 1 and 65535.");

            RuleFor(request => request.From)
                .NotEmpty()
                .WithMessage("Sender address is required.")
                .EmailAddress()
                .WithMessage("Sender address must be valid.");
        });

        When(request => !request.Enabled, () =>
        {
            RuleFor(request => request.Port)
                .InclusiveBetween(1, 65_535)
                .WithMessage("SMTP port must be between 1 and 65535.");
        });
    }
}
