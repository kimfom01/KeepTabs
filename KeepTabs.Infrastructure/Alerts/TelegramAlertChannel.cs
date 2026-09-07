using System.Net.Http.Json;
using System.Text.Json;
using KeepTabs.Application.Alerts;
using KeepTabs.Application.Settings;
using KeepTabs.Domain;
using Microsoft.Extensions.Logging;

namespace KeepTabs.Infrastructure.Alerts;

/// <summary>
/// Delivers alerts through a Telegram bot. The bot token comes from settings;
/// the rule target is the chat ID of the group or channel. Only the delivery
/// outcome is recorded — Telegram's response is never stored.
/// </summary>
public sealed class TelegramAlertChannel : IAlertChannel
{
    private readonly IHttpClientFactory _httpClients;
    private readonly ISettingsService _settings;
    private readonly ILogger<TelegramAlertChannel> _logger;

    public TelegramAlertChannel(
        IHttpClientFactory httpClients,
        ISettingsService settings,
        ILogger<TelegramAlertChannel> logger)
    {
        _httpClients = httpClients;
        _settings = settings;
        _logger = logger;
    }

    public AlertType Type => AlertType.Telegram;

    public async Task<AlertDeliveryResult> SendAsync(
        AlertRule rule,
        Domain.Monitor monitor,
        bool isUp,
        string message,
        CancellationToken cancellationToken = default)
    {
        var telegram = await _settings.GetTelegramAsync(cancellationToken);
        if (!telegram.Enabled || string.IsNullOrWhiteSpace(telegram.BotToken))
        {
            return new AlertDeliveryResult(false, "Telegram delivery is not configured. Set it up in Settings.");
        }

        try
        {
            var client = _httpClients.CreateClient(WebhookAlertChannel.HttpClientName);
            using var response = await client.PostAsJsonAsync(
                $"https://api.telegram.org/bot{telegram.BotToken}/sendMessage",
                new { chat_id = rule.Target, text = message },
                cancellationToken);

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode || !IsOk(payload))
            {
                return new AlertDeliveryResult(false, $"Telegram returned {(int)response.StatusCode}.");
            }

            return new AlertDeliveryResult(true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telegram delivery failed for rule {AlertRuleId}.", rule.Id);

            return new AlertDeliveryResult(false, ex.Message);
        }
    }

    private static bool IsOk(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);

            return document.RootElement.TryGetProperty("ok", out var ok) && ok.GetBoolean();
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
