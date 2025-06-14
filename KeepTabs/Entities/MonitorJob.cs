namespace KeepTabs.Entities;

public record MonitorJob(
    string MonitorId,
    string Url,
    string Title,
    int Interval
);