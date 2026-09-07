using System.Net.Http.Json;
using KeepTabs.Application.Alerts;
using KeepTabs.Domain;
using Microsoft.Extensions.Logging;

namespace KeepTabs.Infrastructure.Alerts;

/// <summary>
/// Delivers alerts by POSTing a JSON payload to the rule's webhook URL.
/// </summary>
public sealed class WebhookAlertChannel : IAlertChannel
{
    public const string HttpClientName = "alerts";

    private readonly IHttpClientFactory _httpClients;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WebhookAlertChannel> _logger;

    public WebhookAlertChannel(
        IHttpClientFactory httpClients,
        TimeProvider timeProvider,
        ILogger<WebhookAlertChannel> logger)
    {
        _httpClients = httpClients;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public AlertType Type => AlertType.Webhook;

    public async Task<AlertDeliveryResult> SendAsync(
        AlertRule rule,
        Domain.Monitor monitor,
        bool isUp,
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClients.CreateClient(HttpClientName);
            using var response = await client.PostAsJsonAsync(
                rule.Target,
                new
                {
                    monitorId = monitor.Id,
                    monitorName = monitor.Name,
                    monitorUrl = monitor.Url,
                    isUp,
                    message,
                    firedAt = _timeProvider.GetUtcNow(),
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new AlertDeliveryResult(false, $"Webhook returned {(int)response.StatusCode}.");
            }

            return new AlertDeliveryResult(true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Webhook delivery failed for rule {AlertRuleId}.", rule.Id);

            return new AlertDeliveryResult(false, ex.Message);
        }
    }
}
