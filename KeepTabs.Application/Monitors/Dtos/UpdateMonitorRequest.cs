namespace KeepTabs.Application.Monitors.Dtos;

/// <summary>Partial update for a monitor. Null properties are left unchanged.</summary>
public sealed record UpdateMonitorRequest(
    string? Name,
    string? Url,
    Domain.ProtocolType? Protocol,
    int? CheckIntervalSeconds,
    int? TimeoutSeconds,
    int? ExpectedStatusCode,
    bool? IsPaused,
    bool? UseHeadRequest = null);
