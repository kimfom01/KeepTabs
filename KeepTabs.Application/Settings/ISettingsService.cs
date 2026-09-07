using KeepTabs.Application.Settings.Dtos;

namespace KeepTabs.Application.Settings;

/// <summary>
/// Effective SMTP configuration used for email alert delivery.
/// </summary>
public sealed record SmtpSettings(
    bool Enabled,
    string Host,
    int Port,
    string Username,
    string Password,
    string From,
    bool EnableSsl);

/// <summary>
/// Effective Telegram configuration used for Telegram alert delivery.
/// </summary>
public sealed record TelegramSettings(
    bool Enabled,
    string BotToken);

/// <summary>
/// Persistent application settings (notification configuration and similar).
/// </summary>
public interface ISettingsService
{
    Task<SmtpSettings> GetSmtpAsync(CancellationToken cancellationToken = default);

    Task<SmtpSettingsResponse> GetSmtpViewAsync(CancellationToken cancellationToken = default);

    Task<SmtpSettingsResponse> UpdateSmtpAsync(
        UpdateSmtpSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<TelegramSettings> GetTelegramAsync(CancellationToken cancellationToken = default);

    Task<TelegramSettingsResponse> GetTelegramViewAsync(CancellationToken cancellationToken = default);

    Task<TelegramSettingsResponse> UpdateTelegramAsync(
        UpdateTelegramSettingsRequest request,
        CancellationToken cancellationToken = default);
}
