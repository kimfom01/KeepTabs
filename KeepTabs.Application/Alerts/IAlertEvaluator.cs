namespace KeepTabs.Application.Alerts;

/// <summary>
/// Evaluates a finished monitor check against the monitor's alert rules and
/// records deliveries. Runs inside the check's unit of work; it never saves.
/// </summary>
public interface IAlertEvaluator
{
    Task EvaluateAsync(
        Domain.Monitor monitor,
        bool? previousStatusUp,
        bool currentIsUp,
        CancellationToken cancellationToken = default);
}
