using KeepTabs.Entities.Base;

namespace KeepTabs.Entities;

public class AlertRule : BaseEntity
{
    public Guid MonitorId { get; set; }
    public Monitor Monitor { get; set; } = default!;

    public AlertType Type { get; set; }
    public AlertTriggerType TriggerType { get; set; }

    public int Threshold { get; set; } = 3;
    public int CoolDownMinutes { get; set; } = 30;
    public bool IsEnabled { get; set; } = true;

    public string Target { get; set; } = default!;
    public DateTimeOffset? LastFiredAt { get; set; }
}

public enum AlertType
{
    Email,
    Webhook,
    Telegram,
}

public enum AlertTriggerType
{
    OnDown,
    OnUp,
    ConsecutiveFailures
}