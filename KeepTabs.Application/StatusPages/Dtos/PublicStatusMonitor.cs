using KeepTabs.Application.Monitors.Dtos;
using KeepTabs.Domain;

namespace KeepTabs.Application.StatusPages.Dtos;

/// <summary>Publicly visible monitor state on a status page.</summary>
public sealed record PublicStatusMonitor(
    Guid MonitorId,
    string Name,
    string Url,
    ProtocolType Protocol,
    bool? LastStatusUp,
    DateTimeOffset? LastCheckedAt,
    double? UptimePercentage,
    IReadOnlyList<HourlyUptimeItem> Hourly,
    IReadOnlyList<DailyUptimeItem> Daily);
