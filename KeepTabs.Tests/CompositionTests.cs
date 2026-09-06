using KeepTabs.Application;
using KeepTabs.Application.Monitoring;
using KeepTabs.Application.Users;
using KeepTabs.Infrastructure;
using KeepTabs.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace KeepTabs.Tests;

public sealed class CompositionTests
{
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
}
