using KeepTabs.Domain;

namespace KeepTabs.Application.Monitors.Dtos;

public sealed record GetMonitorResponse(
    Guid MonitorId,
    string UserId,
    string Name,
    string Url,
    ProtocolType Protocol,
    int CheckIntervalSeconds,
    int TimeoutSeconds,
    int? ExpectedStatusCode,
    bool IsPaused,
    DateTimeOffset? LastCheckedAt,
    bool? LastStatusUp
);