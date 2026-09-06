namespace KeepTabs.Application.Monitors.Dtos;

/// <summary>One persisted monitor-check result.</summary>
public sealed record MonitorCheckHistoryItem(
    Guid CheckId,
    DateTimeOffset Timestamp,
    bool IsUp,
    int? StatusCode,
    int ResponseTimeMs,
    string? ErrorMessage);
