namespace KeepTabs.Application.Monitors.Dtos;

/// <summary>One day of pre-aggregated availability for a monitor.</summary>
public sealed record DailyUptimeItem(
    DateOnly Date,
    int TotalChecks,
    int UpCount,
    double UptimePercentage,
    double AverageResponseTimeMs);
