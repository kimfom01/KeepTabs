using System.Diagnostics;
using KeepTabs.Application.Monitoring;
using KeepTabs.Domain;
using Microsoft.Extensions.Logging;

namespace KeepTabs.Infrastructure.Monitoring;

/// <summary>
/// Executes HTTP availability checks with per-monitor timeouts.
/// </summary>
public sealed class HttpMonitorProbe : IMonitorProbe
{
    private readonly IHttpClientFactory _httpClients;
    private readonly ILogger<HttpMonitorProbe> _logger;

    public HttpMonitorProbe(IHttpClientFactory httpClients, ILogger<HttpMonitorProbe> logger)
    {
        _httpClients = httpClients;
        _logger = logger;
    }

    public bool CanHandle(Domain.ProtocolType protocol) => protocol == Domain.ProtocolType.Http;

    public async Task<MonitorProbeResult> CheckAsync(Domain.Monitor monitor, CancellationToken cancellationToken = default)
    {
        var client = _httpClients.CreateClient(MonitorProbeClients.Checks);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(monitor.TimeoutSeconds));
        var elapsed = Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, monitor.Url);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token);
            elapsed.Stop();

            var statusCode = (int)response.StatusCode;
            var isUp = monitor.ExpectedStatusCode.HasValue
                ? statusCode == monitor.ExpectedStatusCode.Value
                : response.IsSuccessStatusCode;
            var error = isUp || !monitor.ExpectedStatusCode.HasValue
                ? null
                : $"Expected status {monitor.ExpectedStatusCode.Value}, but received {statusCode}.";

            return new MonitorProbeResult(isUp, statusCode, ElapsedMilliseconds(elapsed), error);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            elapsed.Stop();
            _logger.LogWarning(ex, "HTTP check timed out for monitor {MonitorId}.", monitor.Id);

            return new MonitorProbeResult(false, null, ElapsedMilliseconds(elapsed), $"Request timed out after {monitor.TimeoutSeconds} seconds.");
        }
        catch (HttpRequestException ex)
        {
            elapsed.Stop();
            _logger.LogWarning(ex, "HTTP check failed for monitor {MonitorId}.", monitor.Id);

            return new MonitorProbeResult(false, null, ElapsedMilliseconds(elapsed), ex.Message);
        }
    }

    private static int ElapsedMilliseconds(Stopwatch elapsed)
    {
        return (int)Math.Min(elapsed.ElapsedMilliseconds, int.MaxValue);
    }
}
