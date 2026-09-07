using KeepTabs.Application.Alerts;
using KeepTabs.Domain;
using Microsoft.Extensions.Logging;

namespace KeepTabs.Infrastructure.Alerts;

/// <summary>
/// Routes an alert to the channel matching the rule type.
/// </summary>
public sealed class AlertDispatcher : IAlertDispatcher
{
    private readonly IEnumerable<IAlertChannel> _channels;
    private readonly ILogger<AlertDispatcher> _logger;

    public AlertDispatcher(IEnumerable<IAlertChannel> channels, ILogger<AlertDispatcher> logger)
    {
        _channels = channels;
        _logger = logger;
    }

    public Task<AlertDeliveryResult> DispatchAsync(
        AlertRule rule,
        Domain.Monitor monitor,
        bool isUp,
        string message,
        CancellationToken cancellationToken = default)
    {
        var channel = _channels.SingleOrDefault(candidate => candidate.Type == rule.Type);
        if (channel is null)
        {
            _logger.LogError("No alert channel is registered for type {AlertType}.", rule.Type);

            return Task.FromResult(new AlertDeliveryResult(false, $"Alert type {rule.Type} is not supported."));
        }

        return channel.SendAsync(rule, monitor, isUp, message, cancellationToken);
    }
}
