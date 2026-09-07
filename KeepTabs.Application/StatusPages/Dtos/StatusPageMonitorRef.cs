namespace KeepTabs.Application.StatusPages.Dtos;

/// <summary>One monitor entry on a status page.</summary>
public sealed record StatusPageMonitorRef(
    Guid MonitorId,
    string MonitorName);
