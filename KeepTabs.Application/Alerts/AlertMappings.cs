using KeepTabs.Application.Alerts.Dtos;
using KeepTabs.Domain;

namespace KeepTabs.Application.Alerts;

/// <summary>
/// Maps alert requests and entities across the application boundary.
/// </summary>
public static class AlertMappings
{
    public static AlertRule ToEntity(CreateAlertRuleRequest request)
    {
        return new AlertRule
        {
            Id = Guid.CreateVersion7(),
            MonitorId = request.MonitorId,
            Type = request.Type,
            TriggerType = request.TriggerType,
            Threshold = request.Threshold,
            CoolDownMinutes = request.CoolDownMinutes,
            IsEnabled = request.IsEnabled,
            Target = request.Target.Trim(),
        };
    }

    public static GetAlertRuleResponse ToResponse(this AlertRule rule, string monitorName)
    {
        return new GetAlertRuleResponse(
            rule.Id,
            rule.MonitorId,
            monitorName,
            rule.Type,
            rule.TriggerType,
            rule.Threshold,
            rule.CoolDownMinutes,
            rule.IsEnabled,
            rule.Target,
            rule.LastFiredAt
        );
    }
}
