using KeepTabs.Domain;

namespace KeepTabs.Application.Alerts.Dtos;

/// <summary>Payload for creating an alert rule.</summary>
public sealed record CreateAlertRuleRequest(
    Guid MonitorId,
    AlertType Type,
    AlertTriggerType TriggerType,
    int Threshold,
    int CoolDownMinutes,
    string Target,
    bool IsEnabled);
