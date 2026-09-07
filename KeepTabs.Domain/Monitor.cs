using KeepTabs.Domain.Common;

namespace KeepTabs.Domain;

public class Monitor : BaseAuditableEntity
{
    public const int MaxNameLength = 200;
    public const int MaxUrlLength = 1024;

    public required string UserId { get; set; }

    public string Name { get; set; } = default!;
    public string Url { get; set; } = default!;

    public ProtocolType Protocol { get; set; } = ProtocolType.Http;
    public int CheckIntervalSeconds { get; set; } = 60;
    public int TimeoutSeconds { get; set; } = 10;

    public int? ExpectedStatusCode { get; set; }
    public bool UseHeadRequest { get; set; }
    public bool IsPaused { get; set; }

    public DateTimeOffset? LastCheckedAt { get; set; }
    public bool? LastStatusUp { get; set; }

    public ICollection<MonitorCheck> Checks { get; set; } = new List<MonitorCheck>();
    public ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();

    public void ApplyUpdate(
        string name,
        string url,
        ProtocolType protocol,
        int checkIntervalSeconds,
        int timeoutSeconds,
        int? expectedStatusCode,
        bool useHeadRequest,
        bool? isPaused)
    {
        Name = name;
        Url = url;
        Protocol = protocol;
        CheckIntervalSeconds = checkIntervalSeconds;
        TimeoutSeconds = timeoutSeconds;
        ExpectedStatusCode = expectedStatusCode;
        UseHeadRequest = useHeadRequest;

        if (isPaused.HasValue)
        {
            IsPaused = isPaused.Value;
        }
    }

    public void SetPaused(bool isPaused)
    {
        IsPaused = isPaused;
    }

    public MonitorCheck RecordProbeResult(bool isUp, int? statusCode, int responseTimeMs, string? errorMessage, DateTimeOffset checkedAt, int? sslDaysRemaining = null)
    {
        LastCheckedAt = checkedAt;
        LastStatusUp = isUp;

        var check = new MonitorCheck
        {
            Id = Guid.CreateVersion7(),
            MonitorId = Id,
            Timestamp = checkedAt,
            IsUp = isUp,
            StatusCode = statusCode,
            ResponseTimeMs = responseTimeMs,
            ErrorMessage = errorMessage,
            SslDaysRemaining = sslDaysRemaining
        };

        Checks.Add(check);

        return check;
    }
}

public enum ProtocolType
{
    Http,
    Ping,
    Tcp
}

public sealed class MonitorCreateEvent : BaseEvent
{
    public Guid MonitorId { get; init; }
    public required string UserId { get; init; }
}