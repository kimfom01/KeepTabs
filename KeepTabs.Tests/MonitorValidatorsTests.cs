using KeepTabs.Application.Monitors;
using KeepTabs.Application.Monitoring;
using KeepTabs.Application.Monitors.Dtos;
using KeepTabs.Domain;

namespace KeepTabs.Tests;

public sealed class MonitorValidatorsTests
{
    private static CreateMonitorRequest HttpRequest(
        string url = "https://example.com",
        int interval = 60,
        int timeout = 10,
        int? expectedStatus = 200,
        string name = "Site") =>
        new(name, url, ProtocolType.Http, interval, timeout, expectedStatus);

    [Fact]
    public void ValidHttpMonitorPassesValidation()
    {
        var validator = new CreateMonitorRequestValidator();

        Assert.True(validator.Validate(HttpRequest()).IsValid);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    public void InvalidHttpUrlsFailValidation(string url)
    {
        var validator = new CreateMonitorRequestValidator();

        Assert.False(validator.Validate(HttpRequest(url)).IsValid);
    }

    [Fact]
    public void HeadRequestsRequireHttpProtocol()
    {
        var validator = new CreateMonitorRequestValidator();
        var http = HttpRequest() with { UseHeadRequest = true };
        var tcp = new CreateMonitorRequest("TCP service", "example.com:443", ProtocolType.Tcp, 60, 10, null, true);

        Assert.True(validator.Validate(http).IsValid);
        Assert.False(validator.Validate(tcp).IsValid);
    }

    [Fact]
    public void TimeoutMustBeLessThanInterval()
    {
        var validator = new CreateMonitorRequestValidator();

        Assert.False(validator.Validate(HttpRequest(interval: 30, timeout: 30)).IsValid);
    }

    [Theory]
    [InlineData("example.com:443", true)]
    [InlineData("example.com:not-a-port", false)]
    [InlineData("example.com", false)]
    public void TcpEndpointsParseExpectedShapes(string value, bool expected)
    {
        Assert.Equal(expected, TcpEndpoint.TryParse(value, out _));
    }

    [Fact]
    public void ValidTcpRequestPasses()
    {
        var validator = new CreateMonitorRequestValidator();
        var request = new CreateMonitorRequest("TCP service", "example.com:443", ProtocolType.Tcp, 60, 10, null);

        Assert.True(validator.Validate(request).IsValid);
    }

    [Fact]
    public void ValidPingRequestPasses()
    {
        var validator = new CreateMonitorRequestValidator();
        var request = new CreateMonitorRequest("Router", "192.168.1.1", ProtocolType.Ping, 60, 10, null);

        Assert.True(validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("example.com", "TCP monitors require an endpoint in host:port format.")]
    [InlineData("example.com:not-a-port", "TCP monitors require an endpoint in host:port format.")]
    public void InvalidTcpEndpointsFail(string url, string expectedMessage)
    {
        var validator = new CreateMonitorRequestValidator();
        var request = new CreateMonitorRequest("TCP service", url, ProtocolType.Tcp, 60, 10, null);

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == expectedMessage);
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("example.com:80")]
    public void InvalidPingTargetsFail(string url)
    {
        var validator = new CreateMonitorRequestValidator();
        var request = new CreateMonitorRequest("Router", url, ProtocolType.Ping, 60, 10, null);

        Assert.False(validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(29)]
    [InlineData(86_401)]
    public void IntervalOutsideBoundsFails(int interval)
    {
        var validator = new CreateMonitorRequestValidator();

        Assert.False(validator.Validate(HttpRequest(interval: interval)).IsValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(31)]
    public void TimeoutOutsideBoundsFails(int timeout)
    {
        var validator = new CreateMonitorRequestValidator();

        Assert.False(validator.Validate(HttpRequest(timeout: timeout)).IsValid);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(600)]
    public void ExpectedStatusOutsideBoundsFails(int status)
    {
        var validator = new CreateMonitorRequestValidator();

        Assert.False(validator.Validate(HttpRequest(expectedStatus: status)).IsValid);
    }

    [Fact]
    public void MissingExpectedStatusPasses()
    {
        var validator = new CreateMonitorRequestValidator();

        Assert.True(validator.Validate(HttpRequest(expectedStatus: null)).IsValid);
    }

    [Fact]
    public void EmptyNameFails()
    {
        var validator = new CreateMonitorRequestValidator();

        Assert.False(validator.Validate(HttpRequest(name: "")).IsValid);
    }

    [Fact]
    public void OverlongNameFails()
    {
        var validator = new CreateMonitorRequestValidator();

        Assert.False(validator.Validate(HttpRequest(name: new string('n', Domain.Monitor.MaxNameLength + 1))).IsValid);
    }

    [Fact]
    public void UnknownProtocolFails()
    {
        var validator = new CreateMonitorRequestValidator();
        var request = new CreateMonitorRequest("Site", "https://example.com", (ProtocolType)99, 60, 10, null);

        Assert.False(validator.Validate(request).IsValid);
    }

    [Fact]
    public void EmptyUpdatePassesShapeValidation()
    {
        var validator = new UpdateMonitorRequestValidator();
        var request = new UpdateMonitorRequest(null, null, null, null, null, null, null);

        Assert.True(validator.Validate(request).IsValid);
    }

    [Fact]
    public void UpdateValidatesOnlySuppliedFields()
    {
        var validator = new UpdateMonitorRequestValidator();
        var intervalOnly = new UpdateMonitorRequest(null, null, null, 30, null, null, null);
        var timeoutOnly = new UpdateMonitorRequest(null, null, null, null, 30, null, null);

        Assert.True(validator.Validate(intervalOnly).IsValid);
        Assert.True(validator.Validate(timeoutOnly).IsValid);
    }

    [Fact]
    public void UpdateCrossChecksIntervalAndTimeoutWhenBothSupplied()
    {
        var validator = new UpdateMonitorRequestValidator();
        var request = new UpdateMonitorRequest(null, null, null, 30, 30, null, null);

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "Timeout must be less than the check interval.");
    }

    [Fact]
    public void UpdateRejectsBadStatusAndProtocol()
    {
        var validator = new UpdateMonitorRequestValidator();
        var badStatus = new UpdateMonitorRequest(null, null, null, null, null, 99, null);
        var badProtocol = new UpdateMonitorRequest(null, null, (ProtocolType)99, null, null, null, null);

        Assert.False(validator.Validate(badStatus).IsValid);
        Assert.False(validator.Validate(badProtocol).IsValid);
    }

    [Fact]
    public void UpdateRejectsEmptySuppliedName()
    {
        var validator = new UpdateMonitorRequestValidator();
        var request = new UpdateMonitorRequest("", null, null, null, null, null, null);

        Assert.False(validator.Validate(request).IsValid);
    }
}
