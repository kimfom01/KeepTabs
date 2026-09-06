namespace KeepTabs.Application.Monitors.Dtos;

/// <summary>Aggregate availability summary for a monitor.</summary>
public sealed record MonitorSummaryResponse(
    Guid MonitorId,
    string Name,
    string Url,
    int TotalChecks,
    int UpCount,
    int DownCount,
    double UptimePercentage,
    double AverageResponseTimeMs,
    DateTimeOffset? LastCheckedAt,
    bool? LastStatusUp);
