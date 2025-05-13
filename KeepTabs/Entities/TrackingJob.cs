namespace KeepTabs.Entities;

public record TrackingJob(
    string JobId,
    string Url,
    string Title,
    int Interval
);