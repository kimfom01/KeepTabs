using KeepTabs.Domain;

namespace KeepTabs.Application.Alerts.Dtos;

/// <summary>Partial update for an alert rule. Null properties are left unchanged.</summary>
public sealed record UpdateAlertRuleRequest(
    AlertType? Type,
    AlertTriggerType? TriggerType,
    int? Threshold,
    int? CoolDownMinutes,
    string? Target,
    bool? IsEnabled);
