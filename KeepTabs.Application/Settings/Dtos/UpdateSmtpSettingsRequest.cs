namespace KeepTabs.Application.Settings.Dtos;

/// <summary>SMTP configuration payload. An empty password keeps the stored one.</summary>
public sealed record UpdateSmtpSettingsRequest(
    bool Enabled,
    string Host,
    int Port,
    string Username,
    string Password,
    string From,
    bool EnableSsl);
