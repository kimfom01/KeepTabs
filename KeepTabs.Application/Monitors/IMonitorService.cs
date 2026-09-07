using KeepTabs.Application.Monitors.Dtos;

namespace KeepTabs.Application.Monitors;

/// <summary>
/// Monitor use cases. Every operation is scoped to the authenticated owner.
/// </summary>
public interface IMonitorService
{
    Task<GetMonitorResponse> CreateMonitorAsync(
        string userId,
        CreateMonitorRequest request,
        CancellationToken cancellationToken = default);
    Task<GetMonitorResponse?> GetMonitorByIdAsync(
        string userId,
        Guid monitorId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GetMonitorResponse>> GetMonitorsAsync(
        string userId,
        CancellationToken cancellationToken = default);
    Task<GetMonitorResponse?> UpdateMonitorAsync(
        string userId,
        Guid monitorId,
        UpdateMonitorRequest request,
        CancellationToken cancellationToken = default);
    Task<bool> DeleteMonitorAsync(
        string userId,
        Guid monitorId,
        CancellationToken cancellationToken = default);
    Task<GetMonitorResponse?> SetPausedAsync(
        string userId,
        Guid monitorId,
        bool isPaused,
        CancellationToken cancellationToken = default);
    Task<MonitorSummaryResponse?> GetSummaryAsync(
        string userId,
        Guid monitorId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyUptimeItem>> GetDailyAsync(
        string userId,
        Guid monitorId,
        int days,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonitorCheckHistoryItem>> GetHistoryAsync(
        string userId,
        Guid monitorId,
        int days,
        CancellationToken cancellationToken = default);
}
