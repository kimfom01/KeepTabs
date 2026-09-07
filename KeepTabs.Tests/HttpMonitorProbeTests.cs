using System.Net;
using KeepTabs.Domain;
using KeepTabs.Infrastructure.Monitoring;
using Microsoft.Extensions.Logging.Abstractions;
using DomainMonitor = KeepTabs.Domain.Monitor;

namespace KeepTabs.Tests;

public sealed class HttpMonitorProbeTests
{
    [Theory]
    [InlineData(true, "HEAD")]
    [InlineData(false, "GET")]
    public async Task UsesConfiguredHttpMethod(bool useHead, string expectedMethod)
    {
        var factory = new CaptureFactory();
        var probe = new HttpMonitorProbe(factory, NullLogger<HttpMonitorProbe>.Instance);
        var monitor = new DomainMonitor
        {
            UserId = "user-1",
            Name = "Site",
            Url = "http://example.com",
            Protocol = ProtocolType.Http,
            CheckIntervalSeconds = 60,
            TimeoutSeconds = 10,
            ExpectedStatusCode = 200,
            UseHeadRequest = useHead,
        };

        var result = await probe.CheckAsync(monitor);

        Assert.Equal(expectedMethod, factory.LastMethod?.Method);
        Assert.True(result.IsUp);
        Assert.Equal(200, result.StatusCode);
        Assert.Null(result.SslDaysRemaining);
    }

    private sealed class CaptureFactory : IHttpClientFactory
    {
        public HttpMethod? LastMethod { get; private set; }

        public HttpClient CreateClient(string name) => new(new CaptureHandler(this));

        private sealed class CaptureHandler(CaptureFactory parent) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                parent.LastMethod = request.Method;

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }
    }
}
