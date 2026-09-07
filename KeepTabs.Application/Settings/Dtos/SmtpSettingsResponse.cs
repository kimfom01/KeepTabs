namespace KeepTabs.Application.Settings.Dtos;

/// <summary>SMTP configuration. The password itself is never returned.</summary>
public sealed record SmtpSettingsResponse(
    bool Enabled,
    string Host,
    int Port,
    string Username,
    bool PasswordSet,
    string From,
    bool EnableSsl);
