using KeepTabs.Domain;

namespace KeepTabs.Application.Monitoring;

/// <summary>
/// Executes availability probes for one monitor protocol.
/// </summary>
public interface IMonitorProbe
{
    bool CanHandle(ProtocolType protocol);
    Task<MonitorProbeResult> CheckAsync(Domain.Monitor monitor, CancellationToken cancellationToken = default);
}
