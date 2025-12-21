using KeepTabs.Application.Monitors.Dtos;

namespace KeepTabs.Application.Monitors;

public interface IMonitorService
{
    Task<GetMonitorResponse> CreateMonitor(CreateMonitorRequest request, CancellationToken cancellationToken = default);
    Task<GetMonitorResponse?> GetMonitorById(Guid monitorId, CancellationToken cancellationToken);
    IEnumerable<GetMonitorResponse> GetMonitors(string userId);
}