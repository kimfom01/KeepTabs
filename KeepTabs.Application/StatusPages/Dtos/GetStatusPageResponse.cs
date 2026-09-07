namespace KeepTabs.Application.StatusPages.Dtos;

/// <summary>Status page returned by the API.</summary>
public sealed record GetStatusPageResponse(
    Guid StatusPageId,
    string Name,
    string Slug,
    bool IsPublic,
    IReadOnlyList<StatusPageMonitorRef> Monitors);
