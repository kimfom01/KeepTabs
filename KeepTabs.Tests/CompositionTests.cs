using KeepTabs.Application;
using KeepTabs.Application.Monitoring;
using KeepTabs.Application.Users;
using KeepTabs.Infrastructure;
using KeepTabs.Infrastructure.Database;
using KeepTabs.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public void WorkerCompositionResolvesCheckRunner()
    {
        // Guards the full worker graph (runner -> evaluator -> dispatcher ->
        // channels -> settings): descriptor presence alone missed a missing
        // ISettingsService registration that crashed scan jobs at runtime.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMonitorChecking();
        services.AddMonitorCheckingInfrastructure();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused"));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var runner = scope.ServiceProvider.GetRequiredService<IMonitorCheckRunner>();

        Assert.NotNull(runner);
    }
}
