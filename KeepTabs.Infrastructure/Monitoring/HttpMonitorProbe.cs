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

    public bool CanHandle(ProtocolType protocol) => protocol == ProtocolType.Http;

    public async Task<MonitorProbeResult> CheckAsync(Domain.Monitor monitor, CancellationToken cancellationToken = default)
    {
        var client = _httpClients.CreateClient(MonitorProbeClients.Checks);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(monitor.TimeoutSeconds));
        var elapsed = Stopwatch.StartNew();
        var method = monitor.UseHeadRequest ? HttpMethod.Head : HttpMethod.Get;
        var httpsAuthority = HttpsAuthority(monitor.Url);

        try
        {
            using var request = new HttpRequestMessage(method, monitor.Url);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token);
            elapsed.Stop();

            var statusCode = (int)response.StatusCode;
            var isUp = monitor.ExpectedStatusCode.HasValue
                ? statusCode == monitor.ExpectedStatusCode.Value
                : response.IsSuccessStatusCode;
            var error = isUp || !monitor.ExpectedStatusCode.HasValue
                ? null
                : $"Expected status {monitor.ExpectedStatusCode.Value}, but received {statusCode}.";

            return new MonitorProbeResult(
                isUp, statusCode, ElapsedMilliseconds(elapsed), error,
                await SslDaysRemainingAsync(httpsAuthority, cancellationToken));
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            elapsed.Stop();
            _logger.LogWarning(ex, "HTTP check timed out for monitor {MonitorId}.", monitor.Id);

            return new MonitorProbeResult(false, null, ElapsedMilliseconds(elapsed), $"Request timed out after {monitor.TimeoutSeconds} seconds.", await SslDaysRemainingAsync(httpsAuthority, cancellationToken));
        }
        catch (HttpRequestException ex)
        {
            elapsed.Stop();
            _logger.LogWarning(ex, "HTTP check failed for monitor {MonitorId}.", monitor.Id);

            return new MonitorProbeResult(false, null, ElapsedMilliseconds(elapsed), ex.Message, await SslDaysRemainingAsync(httpsAuthority, cancellationToken));
        }
    }

    private static (string Host, int Port)? HttpsAuthority(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
        {
            return (uri.DnsSafeHost, uri.Port);
        }

        return null;
    }

    private static async Task<int?> SslDaysRemainingAsync(
        (string Host, int Port)? authority, CancellationToken cancellationToken)
    {
        if (authority is not { } endpoint)
        {
            return null;
        }

        return await CertificateExpiryReader.GetDaysRemainingAsync(endpoint.Host, endpoint.Port, cancellationToken);
    }

    private static int ElapsedMilliseconds(Stopwatch elapsed)
    {
        return (int)Math.Min(elapsed.ElapsedMilliseconds, int.MaxValue);
    }
}
