namespace KeepTabs.Application.Monitors.Dtos;

/// <summary>Monitor returned by the API.</summary>
public sealed record GetMonitorResponse(
    Guid MonitorId,
    string UserId,
    string Name,
    string Url,
    Domain.ProtocolType Protocol,
    int CheckIntervalSeconds,
    int TimeoutSeconds,
    int? ExpectedStatusCode,
    bool UseHeadRequest,
    bool IsPaused,
    DateTimeOffset? LastCheckedAt,
    bool? LastStatusUp);
