namespace KeepTabs.Application.StatusPages.Dtos;

/// <summary>Partial update for a status page. Null properties are left unchanged.</summary>
public sealed record UpdateStatusPageRequest(
    string? Name,
    string? Slug,
    bool? IsPublic,
    IReadOnlyList<Guid>? MonitorIds);
