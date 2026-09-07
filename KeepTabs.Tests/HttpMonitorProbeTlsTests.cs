using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using KeepTabs.Application.Monitoring;
using KeepTabs.Infrastructure.Monitoring;
using Microsoft.Extensions.Logging.Abstractions;
using DomainMonitor = KeepTabs.Domain.Monitor;

namespace KeepTabs.Tests;

public sealed class HttpMonitorProbeTlsTests : IAsyncLifetime
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _serverLifetime = new();
    private X509Certificate2? _certificate;

    public Task InitializeAsync()
    {
        _listener.Start();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _serverLifetime.CancelAsync();
        _serverLifetime.Dispose();
        _listener.Stop();
        _certificate?.Dispose();
    }

    [Fact]
    public async Task RecordsCertificateExpiryAlongsideCheckResult()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        _certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        var serve = ServeLoopAsync(_serverLifetime.Token);

        var probe = new HttpMonitorProbe(new StubFactory(), NullLogger<HttpMonitorProbe>.Instance);
        var monitor = new DomainMonitor
        {
            UserId = "user-1",
            Name = "TLS",
            Url = $"https://127.0.0.1:{port}/",
            Protocol = KeepTabs.Domain.ProtocolType.Http,
            CheckIntervalSeconds = 60,
            TimeoutSeconds = 10,
        };

        // Self-signed: the check itself fails validation, but the presented
        // certificate expiry is still observed by the dedicated reader.
        var result = await probe.CheckAsync(monitor);

        Assert.False(result.IsUp);
        Assert.NotNull(result.SslDaysRemaining);
        Assert.InRange(result.SslDaysRemaining.Value, 28, 30);

        await _serverLifetime.CancelAsync();
        await serve.WaitAsync(TimeSpan.FromSeconds(15));
    }

    private async Task ServeLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _ = HandleConnectionAsync(client, cancellationToken);
        }
    }

    private async Task HandleConnectionAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        using (var stream = client.GetStream())
        using (var ssl = new SslStream(stream, leaveInnerStreamOpen: true))
        {
            try
            {
                await ssl.AuthenticateAsServerAsync(
                    new SslServerAuthenticationOptions { ServerCertificate = _certificate },
                    cancellationToken);

                var buffer = new byte[4096];
                if (await ssl.ReadAsync(buffer.AsMemory(), cancellationToken) == 0)
                {
                    return;
                }

                var body = "HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray();
                await ssl.WriteAsync(body, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or AuthenticationException or OperationCanceledException)
            {
                // Clients that reject the certificate disconnect; nothing to serve.
            }
        }
    }

    private sealed class StubFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
