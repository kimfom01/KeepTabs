using FluentValidation;
using KeepTabs.Domain;
using KeepTabs.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace KeepTabs.Application.Monitors.Dtos;

public sealed record CreateMonitorRequest(
    string UserId,
    string Name,
    string Url,
    ProtocolType Protocol,
    int CheckIntervalSeconds,
    int TimeoutSeconds,
    int? ExpectedStatusCode
);

public sealed class CreateMonitorRequestValidator : AbstractValidator<CreateMonitorRequest>
{
    public CreateMonitorRequestValidator(ApplicationDbContext dbContext)
    {
        RuleFor(request => request.UserId)
            .MustAsync(async (userId, cancellationToken) =>
            {
                return await dbContext.Users
                    .AnyAsync(user => user.Id == userId, cancellationToken);
            })
            .WithMessage("Invalid User. Please Login or Create an Account");

        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("Monitor {PropertyName} is required")
            .MustAsync(async (request, _, cancellationToken) =>
            {
                return !await dbContext.Monitors
                    .Where(monitor => monitor.Name == request.Name)
                    .Where(monitor => monitor.UserId == request.UserId)
                    .AnyAsync(cancellationToken);
            })
            .WithMessage("Monitor with {PropertyName}: '{PropertyValue}' already exists");

        RuleFor(request => request.Url)
            .NotEmpty()
            .WithMessage("{PropertyName} is required");

        When(request => request.Protocol == ProtocolType.Http, () =>
        {
            RuleFor(request => request.Url)
                .Must(url =>
                {
                    return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
                })
                .WithMessage("HTTP monitor requires a valid http or https URL");
        });
        
        When(request => request.Protocol == ProtocolType.Tcp, () =>
        {
            RuleFor(request => request.Url)
                .Must(value =>
                {
                    var parts = value.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2) return false;

                    var host = parts[0];
                    var portPart = parts[1];

                    if (!int.TryParse(portPart, out var port)) return false;
                    if (port < 1 || port > 65535) return false;

                    return Uri.CheckHostName(host) != UriHostNameType.Unknown;
                })
                .WithMessage("TCP monitor requires endpoint in format host:port");
        });

        When(request => request.Protocol == ProtocolType.Ping, () =>
        {
            RuleFor(request => request.Url)
                .Must(host =>
                    Uri.CheckHostName(host) != UriHostNameType.Unknown)
                .WithMessage("Ping monitor requires a valid hostname or IP address");
        });

        RuleFor(request => request)
            .Must(request =>
            {
                return request.Protocol switch
                {
                    ProtocolType.Http =>
                        request.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                        || request.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase),

                    ProtocolType.Tcp =>
                        request.Url.Contains(':'),

                    ProtocolType.Ping =>
                        !request.Url.Contains("://") && !request.Url.Contains(':'),

                    _ => false
                };
            })
            .WithMessage("URL does not match the selected protocol");


        RuleFor(request => request.Protocol)
            .IsInEnum()
            .WithMessage("Please Provide a Valid {PropertyName}");

        RuleFor(request => request.CheckIntervalSeconds)
            .GreaterThanOrEqualTo(30)
            .WithMessage("Interval must be at least 30 seconds");

        RuleFor(request => request.TimeoutSeconds)
            .InclusiveBetween(2, 30)
            .WithMessage("Timeout must be between 2 and 30 seconds");

        RuleFor(request => request)
            .Must(r => r.TimeoutSeconds < r.CheckIntervalSeconds)
            .WithMessage("Timeout must be less than check interval");
    }
}