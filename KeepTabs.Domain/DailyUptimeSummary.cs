namespace KeepTabs.Domain;

/// <summary>
/// Pre-aggregated per-day availability for one monitor.
/// </summary>
public class DailyUptimeSummary
{
    public Guid MonitorId { get; set; }
    public Monitor Monitor { get; set; } = default!;

    public DateOnly Date { get; set; }
    public int TotalChecks { get; set; }
    public int UpCount { get; set; }
    public double AverageResponseTimeMs { get; set; }
}
