using KeepTabs.Domain;
using DomainMonitor = KeepTabs.Domain.Monitor;

namespace KeepTabs.Tests;

public sealed class MonitorDomainTests
{
    [Fact]
    public void MonitorRecordsProbeResultAndState()
    {
        var monitor = new DomainMonitor { UserId = "user-1", Name = "Site", Url = "https://example.com" };
        var checkedAt = DateTimeOffset.UtcNow;

        var check = monitor.RecordProbeResult(true, 200, 123, null, checkedAt);

        Assert.Equal(checkedAt, monitor.LastCheckedAt);
        Assert.True(monitor.LastStatusUp);
        Assert.Contains(check, monitor.Checks);
    }

    [Fact]
    public void RecordedCheckCarriesProbeData()
    {
        var monitor = new DomainMonitor { UserId = "user-1", Name = "Site", Url = "https://example.com" };

        var check = monitor.RecordProbeResult(false, 500, 1500, "boom", DateTimeOffset.UtcNow);

        Assert.False(check.IsUp);
        Assert.Equal(500, check.StatusCode);
        Assert.Equal(1500, check.ResponseTimeMs);
        Assert.Equal("boom", check.ErrorMessage);
        Assert.Equal(monitor.Id, check.MonitorId);
        Assert.NotEqual(Guid.Empty, check.Id);
        Assert.False(monitor.LastStatusUp);
    }

    [Fact]
    public void ApplyUpdateReplacesEditableFields()
    {
        var monitor = new DomainMonitor
        {
            UserId = "user-1",
            Name = "Old",
            Url = "https://old.example.com",
            Protocol = ProtocolType.Http,
            CheckIntervalSeconds = 60,
            TimeoutSeconds = 10,
            ExpectedStatusCode = 200,
            IsPaused = false,
        };

        monitor.ApplyUpdate("New", "example.com:443", ProtocolType.Tcp, 120, 20, null, true);

        Assert.Equal("New", monitor.Name);
        Assert.Equal("example.com:443", monitor.Url);
        Assert.Equal(ProtocolType.Tcp, monitor.Protocol);
        Assert.Equal(120, monitor.CheckIntervalSeconds);
        Assert.Equal(20, monitor.TimeoutSeconds);
        Assert.Null(monitor.ExpectedStatusCode);
        Assert.True(monitor.IsPaused);
    }

    [Fact]
    public void ApplyUpdateLeavesPauseUnchangedWhenNotSupplied()
    {
        var monitor = new DomainMonitor
        {
            UserId = "user-1",
            Name = "Site",
            Url = "https://example.com",
            IsPaused = true,
        };

        monitor.ApplyUpdate("Site", "https://example.com", ProtocolType.Http, 60, 10, 200, null);

        Assert.True(monitor.IsPaused);
    }

    [Fact]
    public void SetPausedToggles()
    {
        var monitor = new DomainMonitor { UserId = "user-1", Name = "Site", Url = "https://example.com" };

        monitor.SetPaused(true);

        Assert.True(monitor.IsPaused);

        monitor.SetPaused(false);

        Assert.False(monitor.IsPaused);
    }
}
