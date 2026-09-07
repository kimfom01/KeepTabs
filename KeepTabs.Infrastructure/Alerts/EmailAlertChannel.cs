using System.Net;
using System.Net.Mail;
using KeepTabs.Application.Alerts;
using KeepTabs.Application.Settings;
using KeepTabs.Domain;

namespace KeepTabs.Infrastructure.Alerts;

/// <summary>
/// Delivers alerts by email using the stored SMTP configuration.
/// Misconfiguration is reported per delivery instead of failing startup.
/// </summary>
public sealed class EmailAlertChannel : IAlertChannel
{
    private readonly ISettingsService _settings;

    public EmailAlertChannel(ISettingsService settings)
    {
        _settings = settings;
    }

    public AlertType Type => AlertType.Email;

    public async Task<AlertDeliveryResult> SendAsync(
        AlertRule rule,
        Domain.Monitor monitor,
        bool isUp,
        string message,
        CancellationToken cancellationToken = default)
    {
        var smtp = await _settings.GetSmtpAsync(cancellationToken);
        if (!smtp.Enabled || string.IsNullOrWhiteSpace(smtp.Host))
        {
            return new AlertDeliveryResult(false, "SMTP delivery is not configured. Set it up in Settings.");
        }

        if (string.IsNullOrWhiteSpace(smtp.From))
        {
            return new AlertDeliveryResult(false, "SMTP sender address is not configured.");
        }

        try
        {
            using var client = new SmtpClient(smtp.Host, smtp.Port)
            {
                EnableSsl = smtp.EnableSsl,
                Credentials = string.IsNullOrWhiteSpace(smtp.Username)
                    ? null
                    : new NetworkCredential(smtp.Username, smtp.Password),
            };
            using var mail = new MailMessage(smtp.From, rule.Target)
            {
                Subject = $"[KeepTabs] {monitor.Name} is {(isUp ? "UP" : "DOWN")}",
                Body = message,
            };

            await client.SendMailAsync(mail, cancellationToken);

            return new AlertDeliveryResult(true, null);
        }
        catch (Exception ex)
        {
            return new AlertDeliveryResult(false, ex.Message);
        }
    }
}
