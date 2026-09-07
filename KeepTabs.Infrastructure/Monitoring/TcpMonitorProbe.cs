using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using KeepTabs.Application.Monitoring;
using Microsoft.Extensions.Logging;

namespace KeepTabs.Infrastructure.Monitoring;

/// <summary>
/// Executes TCP port checks with per-monitor timeouts.
/// </summary>
public sealed class TcpMonitorProbe : IMonitorProbe
{
    private readonly ILogger<TcpMonitorProbe> _logger;

    public TcpMonitorProbe(ILogger<TcpMonitorProbe> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(Domain.ProtocolType protocol) => protocol == Domain.ProtocolType.Tcp;

    public async Task<MonitorProbeResult> CheckAsync(Domain.Monitor monitor, CancellationToken cancellationToken = default)
    {
        if (!TcpEndpoint.TryParse(monitor.Url, out var endpoint) || endpoint is null)
        {
            return new MonitorProbeResult(false, null, 0, "TCP monitors require an endpoint in host:port format.");
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(monitor.TimeoutSeconds));
        var elapsed = Stopwatch.StartNew();

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(endpoint.Host, timeoutSource.Token);
            Exception? lastError = null;

            foreach (var address in addresses)
            {
                using var client = new TcpClient();
                try
                {
                    await client.ConnectAsync(address, endpoint.Port).WaitAsync(timeoutSource.Token);
                    elapsed.Stop();

                    return new MonitorProbeResult(true, null, ElapsedMilliseconds(elapsed), null);
                }
                catch (Exception ex) when (ex is SocketException or OperationCanceledException or InvalidOperationException)
                {
                    lastError = ex;
                }
            }

            elapsed.Stop();

            return new MonitorProbeResult(false, null, ElapsedMilliseconds(elapsed), lastError?.Message ?? "TCP connection failed.");
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            elapsed.Stop();
            _logger.LogWarning(ex, "TCP check timed out for monitor {MonitorId}.", monitor.Id);

            return new MonitorProbeResult(false, null, ElapsedMilliseconds(elapsed), $"Connection timed out after {monitor.TimeoutSeconds} seconds.");
        }
        catch (SocketException ex)
        {
            elapsed.Stop();
            _logger.LogWarning(ex, "TCP check failed for monitor {MonitorId}.", monitor.Id);

            return new MonitorProbeResult(false, null, ElapsedMilliseconds(elapsed), ex.Message);
        }
    }

    private static int ElapsedMilliseconds(Stopwatch elapsed)
    {
        return (int)Math.Min(elapsed.ElapsedMilliseconds, int.MaxValue);
    }
}
