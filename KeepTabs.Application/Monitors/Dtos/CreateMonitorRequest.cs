namespace KeepTabs.Application.Monitors.Dtos;

/// <summary>Payload for creating a monitor. The owner is taken from authentication.</summary>
public sealed record CreateMonitorRequest(
    string Name,
    string Url,
    Domain.ProtocolType Protocol,
    int CheckIntervalSeconds,
    int TimeoutSeconds,
    int? ExpectedStatusCode,
    bool UseHeadRequest = false);
