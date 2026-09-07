namespace KeepTabs.Application.StatusPages.Dtos;

/// <summary>One hour of pre-aggregated availability for a monitor.</summary>
public sealed record HourlyUptimeItem(
    DateTimeOffset Hour,
    int TotalChecks,
    int UpCount,
    double UptimePercentage,
    double AverageResponseTimeMs);
