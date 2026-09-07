namespace KeepTabs.Application.Alerts.Dtos;

/// <summary>One fired alert delivery attempt.</summary>
public sealed record AlertLogResponse(
    Guid AlertLogId,
    Guid AlertRuleId,
    Guid MonitorId,
    string MonitorName,
    DateTimeOffset FiredAt,
    string Message,
    bool Success,
    string? Error);
