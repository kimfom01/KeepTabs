namespace KeepTabs.Application.StatusPages.Dtos;

/// <summary>Payload for creating a status page. An empty slug is generated from the name.</summary>
public sealed record CreateStatusPageRequest(
    string Name,
    string? Slug,
    bool IsPublic,
    IReadOnlyList<Guid> MonitorIds);
