using KeepTabs.Domain.Common;

namespace KeepTabs.Domain;

/// <summary>
/// A public status page grouping selected monitors under a slug.
/// </summary>
public class StatusPage : BaseAuditableEntity
{
    public const int MaxNameLength = 200;
    public const int MaxSlugLength = 100;

    public required string UserId { get; set; }

    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public bool IsPublic { get; set; }

    public ICollection<StatusPageMonitor> Monitors { get; set; } = new List<StatusPageMonitor>();
}

/// <summary>
/// One monitor on a status page, with explicit ordering.
/// </summary>
public class StatusPageMonitor
{
    public Guid StatusPageId { get; set; }
    public StatusPage StatusPage { get; set; } = default!;

    public Guid MonitorId { get; set; }
    public Monitor Monitor { get; set; } = default!;

    public int SortOrder { get; set; }
}
