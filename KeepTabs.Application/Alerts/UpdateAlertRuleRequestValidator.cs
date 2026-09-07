using FluentValidation;
using KeepTabs.Application.Alerts.Dtos;
using KeepTabs.Domain;

namespace KeepTabs.Application.Alerts;

/// <summary>
/// Format and range validation for supplied alert-rule update fields.
/// </summary>
public sealed class UpdateAlertRuleRequestValidator : AbstractValidator<UpdateAlertRuleRequest>
{
    public UpdateAlertRuleRequestValidator()
    {
        When(request => request.Type is not null, () =>
        {
            RuleFor(request => request.Type!.Value)
                .IsInEnum()
                .WithMessage("A valid alert type is required.");
        });

        When(request => request.TriggerType is not null, () =>
        {
            RuleFor(request => request.TriggerType!.Value)
                .IsInEnum()
                .WithMessage("A valid trigger is required.");
        });

        When(request => request.Threshold is not null, () =>
        {
            RuleFor(request => request.Threshold!.Value)
                .InclusiveBetween(1, 100)
                .WithMessage("Threshold must be between 1 and 100 consecutive failures.");
        });

        When(request => request.CoolDownMinutes is not null, () =>
        {
            RuleFor(request => request.CoolDownMinutes!.Value)
                .InclusiveBetween(0, 1_440)
                .WithMessage("Cooldown must be between 0 minutes and 24 hours.");
        });

        When(request => request.Target is not null, () =>
        {
            RuleFor(request => request.Target!)
                .NotEmpty()
                .WithMessage("Target is required.")
                .MaximumLength(512);
        });
    }
}
