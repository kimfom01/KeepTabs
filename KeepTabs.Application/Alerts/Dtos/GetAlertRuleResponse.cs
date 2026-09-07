using KeepTabs.Domain;

namespace KeepTabs.Application.Alerts.Dtos;

/// <summary>Alert rule returned by the API.</summary>
public sealed record GetAlertRuleResponse(
    Guid AlertRuleId,
    Guid MonitorId,
    string MonitorName,
    AlertType Type,
    AlertTriggerType TriggerType,
    int Threshold,
    int CoolDownMinutes,
    bool IsEnabled,
    string Target,
    DateTimeOffset? LastFiredAt);
