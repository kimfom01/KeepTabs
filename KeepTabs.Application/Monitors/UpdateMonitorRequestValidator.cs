using FluentValidation;
using KeepTabs.Application.Monitors.Dtos;

namespace KeepTabs.Application.Monitors;

/// <summary>
/// Format and range validation for supplied monitor-update fields.
/// </summary>
public sealed class UpdateMonitorRequestValidator : AbstractValidator<UpdateMonitorRequest>
{
    public UpdateMonitorRequestValidator()
    {
        When(request => request.Name is not null, () =>
        {
            RuleFor(request => request.Name!)
                .NotEmpty()
                .WithMessage("Monitor name is required.")
                .MaximumLength(Domain.Monitor.MaxNameLength);
        });

        When(request => request.Url is not null, () =>
        {
            RuleFor(request => request.Url!)
                .NotEmpty()
                .WithMessage("Url is required.")
                .MaximumLength(Domain.Monitor.MaxUrlLength);
        });

        When(request => request.Protocol is not null, () =>
        {
            RuleFor(request => request.Protocol!.Value)
                .IsInEnum()
                .WithMessage("A valid protocol is required.");
        });

        When(request => request.CheckIntervalSeconds is not null, () =>
        {
            RuleFor(request => request.CheckIntervalSeconds!.Value)
                .GreaterThanOrEqualTo(30)
                .WithMessage("Interval must be at least 30 seconds.")
                .LessThanOrEqualTo(86_400)
                .WithMessage("Interval must be no more than 24 hours.");
        });

        When(request => request.TimeoutSeconds is not null, () =>
        {
            RuleFor(request => request.TimeoutSeconds!.Value)
                .InclusiveBetween(2, 30)
                .WithMessage("Timeout must be between 2 and 30 seconds.");
        });

        When(request => request.CheckIntervalSeconds.HasValue && request.TimeoutSeconds.HasValue, () =>
        {
            RuleFor(request => request)
                .Must(request => request.TimeoutSeconds!.Value < request.CheckIntervalSeconds!.Value)
                .WithMessage("Timeout must be less than the check interval.");
        });

        When(request => request.ExpectedStatusCode.HasValue, () =>
        {
            RuleFor(request => request.ExpectedStatusCode!.Value)
                .InclusiveBetween(100, 599)
                .WithMessage("Expected status code must be between 100 and 599.");
        });
    }
}
