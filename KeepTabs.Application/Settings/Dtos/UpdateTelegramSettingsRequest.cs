namespace KeepTabs.Application.Settings.Dtos;

/// <summary>Telegram configuration payload. An empty token keeps the stored one.</summary>
public sealed record UpdateTelegramSettingsRequest(
    bool Enabled,
    string BotToken);
