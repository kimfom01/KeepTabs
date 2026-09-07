namespace KeepTabs.Application.Alerts.Dtos;

/// <summary>Outcome of a manually triggered test delivery.</summary>
public sealed record TestAlertResponse(
    bool Success,
    string? Error,
    string Message);
