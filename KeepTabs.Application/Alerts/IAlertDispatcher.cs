using KeepTabs.Domain;

namespace KeepTabs.Application.Alerts;

/// <summary>
/// Delivery outcome for one alert channel attempt.
/// </summary>
public sealed record AlertDeliveryResult(bool Success, string? Error);

/// <summary>
/// Sends one alert through its configured channel.
/// </summary>
public interface IAlertDispatcher
{
    Task<AlertDeliveryResult> DispatchAsync(
        AlertRule rule,
        Domain.Monitor monitor,
        bool isUp,
        string message,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// One delivery channel (email, webhook, ...).
/// </summary>
public interface IAlertChannel
{
    AlertType Type { get; }

    Task<AlertDeliveryResult> SendAsync(
        AlertRule rule,
        Domain.Monitor monitor,
        bool isUp,
        string message,
        CancellationToken cancellationToken = default);
}
