namespace KeepTabs.Application.Monitoring;

/// <summary>
/// Runs scheduled monitor checks and persists their results.
/// </summary>
public interface IMonitorCheckRunner
{
    Task<IReadOnlyList<Guid>> GetDueMonitorIdsAsync(CancellationToken cancellationToken = default);
    Task<int> RunDueChecksAsync(CancellationToken cancellationToken = default);
    Task<bool> RunCheckAsync(Guid monitorId, CancellationToken cancellationToken = default);
}
