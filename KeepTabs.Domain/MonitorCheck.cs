using KeepTabs.Domain.Common;

namespace KeepTabs.Domain;

public class MonitorCheck : BaseAuditableEntity
{
    public Guid MonitorId { get; set; }
    public Monitor Monitor { get; set; } = default!;

    public DateTimeOffset Timestamp { get; set; }
    public bool IsUp { get; set; }
    public int? StatusCode { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}