namespace KeepTabs.Application.StatusPages.Dtos;

/// <summary>Public status page payload. Contains no owner information.</summary>
public sealed record PublicStatusPageResponse(
    string Name,
    string Slug,
    IReadOnlyList<PublicStatusMonitor> Monitors);
