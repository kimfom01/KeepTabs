using System.Diagnostics;
using System.Net.NetworkInformation;
using KeepTabs.Application.Monitoring;
using KeepTabs.Domain;
using Microsoft.Extensions.Logging;

namespace KeepTabs.Infrastructure.Monitoring;

/// <summary>
/// Executes ICMP ping checks with per-monitor timeouts.
/// </summary>
public sealed class PingMonitorProbe : IMonitorProbe
{
    private readonly ILogger<PingMonitorProbe> _logger;

    public PingMonitorProbe(ILogger<PingMonitorProbe> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(ProtocolType protocol) => protocol == ProtocolType.Ping;

    public async Task<MonitorProbeResult> CheckAsync(Domain.Monitor monitor, CancellationToken cancellationToken = default)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(monitor.TimeoutSeconds));
        var elapsed = Stopwatch.StartNew();

        try
        {
            using var ping = new Ping();
            var reply = await ping
                .SendPingAsync(monitor.Url.Trim(), (int)TimeSpan.FromSeconds(monitor.TimeoutSeconds).TotalMilliseconds)
                .WaitAsync(timeoutSource.Token);
            elapsed.Stop();

            if (reply.Status == IPStatus.Success)
            {
                return new MonitorProbeResult(true, null, ElapsedMilliseconds(elapsed), null);
            }

            return new MonitorProbeResult(false, null, ElapsedMilliseconds(elapsed), $"Ping failed with status {reply.Status}.");
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            elapsed.Stop();
            _logger.LogWarning(ex, "Ping check timed out for monitor {MonitorId}.", monitor.Id);

            return new MonitorProbeResult(false, null, ElapsedMilliseconds(elapsed), $"Ping timed out after {monitor.TimeoutSeconds} seconds.");
        }
        catch (PingException ex)
        {
            elapsed.Stop();
            _logger.LogWarning(ex, "Ping check failed for monitor {MonitorId}.", monitor.Id);

            return new MonitorProbeResult(false, null, ElapsedMilliseconds(elapsed), ex.Message);
        }
    }

    private static int ElapsedMilliseconds(Stopwatch elapsed)
    {
        return (int)Math.Min(elapsed.ElapsedMilliseconds, int.MaxValue);
    }
}
