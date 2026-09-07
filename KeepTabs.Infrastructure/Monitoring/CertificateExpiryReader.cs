using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace KeepTabs.Infrastructure.Monitoring;

/// <summary>
/// Reads certificate expiry with a dedicated short TLS handshake. The main
/// check keeps using the shared client untouched, so validation semantics
/// and response-time measurement are unaffected.
/// </summary>
internal static class CertificateExpiryReader
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public static async Task<int?> GetDaysRemainingAsync(string host, int port, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(Timeout);

        TcpClient? client = null;
        try
        {
            client = new TcpClient();
            await client.ConnectAsync(host, port, timeoutSource.Token);
            // Synchronous dispose: async SslStream shutdown waits for a peer
            // close-notify that may never arrive.
            using var stream = client.GetStream();
            using var ssl = new SslStream(stream, leaveInnerStreamOpen: true);
            await ssl.AuthenticateAsClientAsync(
                new SslClientAuthenticationOptions
                {
                    TargetHost = host,
                    RemoteCertificateValidationCallback = (_, _, _, _) => true,
                },
                timeoutSource.Token);

            if (ssl.RemoteCertificate is not { } certificate)
            {
                return null;
            }

            using var copy = new X509Certificate2(certificate);
            var notAfter = copy.NotAfter.ToUniversalTime();

            return (int)Math.Floor((notAfter - DateTimeOffset.UtcNow).TotalDays);
        }
        catch
        {
            return null;
        }
        finally
        {
            client?.Close();
        }
    }
}
