namespace KeepTabs.Application.Settings.Dtos;

/// <summary>Telegram configuration. The token itself is never returned.</summary>
public sealed record TelegramSettingsResponse(
    bool Enabled,
    bool TokenSet);
