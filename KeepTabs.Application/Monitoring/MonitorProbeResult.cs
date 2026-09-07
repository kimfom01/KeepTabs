namespace KeepTabs.Application.Monitoring;

/// <summary>
/// Result of executing one monitor probe.
/// </summary>
public sealed record MonitorProbeResult(
    bool IsUp,
    int? StatusCode,
    int ResponseTimeMs,
    string? ErrorMessage,
    int? SslDaysRemaining = null);
