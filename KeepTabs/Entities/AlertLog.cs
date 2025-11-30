using KeepTabs.Entities.Base;

namespace KeepTabs.Entities;

public class AlertLog : BaseEntity
{
    public Guid AlertRuleId { get; set; }
    public AlertRule AlertRule { get; set; } = default!;

    public DateTimeOffset FiredAt { get; set; }
    public string Message { get; set; } = default!;
    public bool Success { get; set; }
    public string? Error { get; set; }
}