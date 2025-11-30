using KeepTabs.Entities.Base;

namespace KeepTabs.Entities;

public class Monitor : BaseEntity
{
    public string UserId { get; set; }

    public string Name { get; set; } = default!;
    public string Url { get; set; } = default!;

    public ProtocolType Protocol { get; set; } = ProtocolType.Http;
    public int CheckIntervalSeconds { get; set; } = 60;
    public int TimeoutSeconds { get; set; } = 10;

    public int? ExpectedStatusCode { get; set; }
    public bool IsPaused { get; set; }
    
    public DateTimeOffset? LastCheckedAt { get; set; }
    public bool? LastStatusUp { get; set; }

    public ICollection<MonitorCheck> Checks { get; set; } = new List<MonitorCheck>();
    public ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();
}

public enum ProtocolType
{
    Http,
    Ping,
    Tcp
}