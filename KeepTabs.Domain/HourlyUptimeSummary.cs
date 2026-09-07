namespace KeepTabs.Domain;

/// <summary>
/// Pre-aggregated per-hour availability for one monitor.
/// </summary>
public class HourlyUptimeSummary
{
    public Guid MonitorId { get; set; }
    public Monitor Monitor { get; set; } = default!;

    public DateTimeOffset Hour { get; set; }
    public int TotalChecks { get; set; }
    public int UpCount { get; set; }
    public double AverageResponseTimeMs { get; set; }
}
