using KeepTabs.Application;
using KeepTabs.Application.Monitors;
using KeepTabs.Application.Monitors.Dtos;
using KeepTabs.Application.Users.Dtos;
using KeepTabs.Application.Monitoring;
using KeepTabs.Application.Users;
using KeepTabs.Domain;
using KeepTabs.Extensions;
using KeepTabs.Infrastructure;
using KeepTabs.Infrastructure.Identity;
using KeepTabs.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using DomainMonitor = KeepTabs.Domain.Monitor;

namespace KeepTabs.Tests;

public sealed class StandardsTests
{
    [Fact]
    public void ValidHttpMonitorPassesValidation()
    {
        var validator = new CreateMonitorRequestValidator();
        var request = new CreateMonitorRequest(
            "Marketing site",
            "https://example.com",
            ProtocolType.Http,
            60,
            10,
            200);

        Assert.True(validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    public void InvalidHttpUrlsFailValidation(string url)
    {
        var validator = new CreateMonitorRequestValidator();
        var request = new CreateMonitorRequest(
            "Marketing site",
            url,
            ProtocolType.Http,
            60,
            10,
            null);

        Assert.False(validator.Validate(request).IsValid);
    }

    [Fact]
    public void TimeoutMustBeLessThanInterval()
    {
        var validator = new CreateMonitorRequestValidator();
        var request = new CreateMonitorRequest(
            "Marketing site",
            "https://example.com",
            ProtocolType.Http,
            30,
            30,
            null);

        Assert.False(validator.Validate(request).IsValid);
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
    public void ApiKeysAreUrlSafeAndHashesAreStable()
    {
        var service = new ApiKeyService();
        var rawApiKey = service.GenerateRawApiKey();

        Assert.Equal(43, rawApiKey.Length);
        Assert.Equal(rawApiKey, rawApiKey.TrimEnd('='));
        Assert.Equal(44, service.ComputeHash(rawApiKey).Length);
        Assert.Equal(service.ComputeHash(rawApiKey), service.ComputeHash(rawApiKey));
        Assert.NotEqual(service.ComputeHash(rawApiKey), service.ComputeHash(service.GenerateRawApiKey()));
    }

    [Fact]
    public void RegistrationRejectsWeakCredentials()
    {
        var validator = new RegisterRequestValidator();
        var request = new RegisterRequest("not-an-email", "short", null, null);

        Assert.False(validator.Validate(request).IsValid);
    }

    [Fact]
    public void WorkerCompositionExcludesIdentityAndUserAccounts()
    {
        var services = new ServiceCollection();
        services.AddMonitorChecking();
        services.AddMonitorCheckingInfrastructure();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IMonitorCheckRunner));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IMonitorProbe));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IUserAccountStore));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(UserManager<ApplicationUser>));
        Assert.DoesNotContain(services, descriptor => descriptor.ImplementationType == typeof(AspNetIdentityUserAccountStore));
    }

    [Fact]
    public void ApiCompositionIncludesIdentityAndUserAccounts()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddInfrastructureServices();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IMonitorCheckRunner));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IAuthService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IUserService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IUserAccountStore));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(UserManager<ApplicationUser>));
    }

    [Theory]
    [InlineData("https://app.example.com", "https://app.example.com")]
    [InlineData("https://a.example.com,https://b.example.com", "https://a.example.com|https://b.example.com")]
    [InlineData("  https://a.example.com , https://b.example.com/ ", "https://a.example.com|https://b.example.com")]
    [InlineData("https://a.example.com,https://a.example.com,HTTPS://A.EXAMPLE.COM", "https://a.example.com")]
    public void AllowedOriginsParseFromCommaSeparatedValues(string raw, string expectedPipeJoined)
    {
        Assert.Equal(expectedPipeJoined.Split('|'), CorsOptions.ParseAllowedOrigins(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",,,")]
    public void AllowedOriginsFallBackToDefaultWhenUnset(string? raw)
    {
        Assert.Equal(CorsOptions.DefaultAllowedOrigins, CorsOptions.ParseAllowedOrigins(raw));
    }

    [Fact]
    public void AllowedOriginsNormalizeArraySyntax()
    {
        var normalized = CorsOptions.NormalizeOrigins(
            ["https://a.example.com/", " https://b.example.com ", null, "", "https://a.example.com"]);

        Assert.Equal(["https://a.example.com", "https://b.example.com"], normalized);
    }
}
