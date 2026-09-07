using FluentValidation;
using KeepTabs.Application.Alerts.Dtos;
using KeepTabs.Domain;

namespace KeepTabs.Application.Alerts;

/// <summary>
/// Format and range validation for alert-rule creation. Ownership is checked by the service.
/// </summary>
public sealed class CreateAlertRuleRequestValidator : AbstractValidator<CreateAlertRuleRequest>
{
    public CreateAlertRuleRequestValidator()
    {
        RuleFor(request => request.MonitorId)
            .NotEmpty()
            .WithMessage("Monitor is required.");

        RuleFor(request => request.Type)
            .IsInEnum()
            .WithMessage("A valid alert type is required.");

        RuleFor(request => request.TriggerType)
            .IsInEnum()
            .WithMessage("A valid trigger is required.");

        RuleFor(request => request.Threshold)
            .InclusiveBetween(1, 100)
            .WithMessage("Threshold must be between 1 and 100 consecutive failures.");

        RuleFor(request => request.CoolDownMinutes)
            .InclusiveBetween(0, 1_440)
            .WithMessage("Cooldown must be between 0 minutes and 24 hours.");

        RuleFor(request => request.Target)
            .NotEmpty()
            .WithMessage("Target is required.")
            .MaximumLength(512);

        When(request => request.Type == AlertType.Email, () =>
        {
            RuleFor(request => request.Target)
                .EmailAddress()
                .WithMessage("Email alerts require a valid email address.");
        });

        When(request => request.Type == AlertType.Webhook, () =>
        {
            RuleFor(request => request.Target)
                .Must(target => Uri.TryCreate(target, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .WithMessage("Webhook alerts require a valid http or https URL.");
        });

        When(request => request.Type == AlertType.Telegram, () =>
        {
            RuleFor(request => request.Target)
                .Must(target => long.TryParse(target, out _) || IsChannelHandle(target))
                .WithMessage("Telegram alerts require a chat ID (e.g. -1001234567890) or channel handle (e.g. @alerts).");
        });
    }

    private static bool IsChannelHandle(string target)
    {
        return target.StartsWith('@') && target.Length is >= 6 and <= 33;
    }
}
