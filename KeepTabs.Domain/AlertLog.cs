using KeepTabs.Domain.Common;

namespace KeepTabs.Domain;

public class AlertLog : BaseAuditableEntity
{
    public Guid AlertRuleId { get; set; }
    public AlertRule AlertRule { get; set; } = default!;

    public DateTimeOffset FiredAt { get; set; }
    public string Message { get; set; } = default!;
    public bool Success { get; set; }
    public string? Error { get; set; }
}