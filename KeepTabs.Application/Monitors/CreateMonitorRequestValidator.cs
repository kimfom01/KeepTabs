using FluentValidation;
using KeepTabs.Application.Monitors.Dtos;
using KeepTabs.Domain;

namespace KeepTabs.Application.Monitors;

/// <summary>
/// Format and range validation for monitor creation. Ownership and uniqueness are checked by the service.
/// </summary>
public sealed class CreateMonitorRequestValidator : AbstractValidator<CreateMonitorRequest>
{
    public CreateMonitorRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("Monitor name is required.")
            .MaximumLength(Domain.Monitor.MaxNameLength);

        RuleFor(request => request.Url)
            .NotEmpty()
            .WithMessage("Url is required.")
            .MaximumLength(Domain.Monitor.MaxUrlLength);

        RuleFor(request => request.Protocol)
            .IsInEnum()
            .WithMessage("A valid protocol is required.");

        When(request => request.Protocol == ProtocolType.Http, () =>
        {
            RuleFor(request => request.Url)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .WithMessage("HTTP monitors require a valid http or https URL.");
        });

        When(request => request.Protocol == ProtocolType.Tcp, () =>
        {
            RuleFor(request => request.Url)
                .Must(MonitorUrlRules.IsTcpEndpoint)
                .WithMessage("TCP monitors require an endpoint in host:port format.");
        });

        When(request => request.Protocol == ProtocolType.Ping, () =>
        {
            RuleFor(request => request.Url)
                .Must(MonitorUrlRules.IsPingTarget)
                .WithMessage("Ping monitors require a valid hostname or IP address.");
        });

        RuleFor(request => request.CheckIntervalSeconds)
            .GreaterThanOrEqualTo(30)
            .WithMessage("Interval must be at least 30 seconds.")
            .LessThanOrEqualTo(86_400)
            .WithMessage("Interval must be no more than 24 hours.");

        RuleFor(request => request.TimeoutSeconds)
            .InclusiveBetween(2, 30)
            .WithMessage("Timeout must be between 2 and 30 seconds.");

        RuleFor(request => request)
            .Must(request => request.TimeoutSeconds < request.CheckIntervalSeconds)
            .WithMessage("Timeout must be less than the check interval.");

        When(request => request.ExpectedStatusCode.HasValue, () =>
        {
            RuleFor(request => request.ExpectedStatusCode!.Value)
                .InclusiveBetween(100, 599)
                .WithMessage("Expected status code must be between 100 and 599.");
        });
    }
}
