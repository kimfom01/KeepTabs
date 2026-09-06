using System.Reflection;
using KeepTabs.Application.Common.Interfaces;
using KeepTabs.Application.Users;
using KeepTabs.Infrastructure.Database;
using KeepTabs.Infrastructure.Database.Interceptors;
using KeepTabs.Infrastructure.Identity;
using KeepTabs.Infrastructure.Monitoring;
using KeepTabs.Infrastructure.Security;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace KeepTabs.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddMonitorCheckingInfrastructure();
        services.AddIdentityInfrastructure();
    }

    public static void AddMonitorCheckingInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<DispatchDomainEventsInterceptor>();
        services.AddSingleton<IConfigureOptions<DbContextOptions<ApplicationDbContext>>, PersistenceInterceptorOptionsSetup>();
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        services.AddTransient<Application.Monitoring.IMonitorProbe, HttpMonitorProbe>();
        services.AddTransient<Application.Monitoring.IMonitorProbe, TcpMonitorProbe>();
        services.AddTransient<Application.Monitoring.IMonitorProbe, PingMonitorProbe>();
        services.AddHttpClient(MonitorProbeClients.Checks, client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.RegisterServicesFromAssembly(typeof(IApplicationDbContext).Assembly);
        });
    }

    public static void AddIdentityInfrastructure(this IServiceCollection services)
    {
        services.AddKeepTabsIdentity();
        services.AddScoped<IUserAccountStore, AspNetIdentityUserAccountStore>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IApiKeyService, ApiKeyService>();
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => !string.IsNullOrWhiteSpace(options.Key) && options.Key.Length >= 32, "Jwt:Key must contain at least 32 characters.");
    }

    public static void AddKeepTabsIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
    }

    public static IHostApplicationBuilder AddKeepTabsPersistence(
        this IHostApplicationBuilder builder,
        string connectionName = "keeptabsdb")
    {
        builder.AddNpgsqlDbContext<ApplicationDbContext>(connectionName);

        return builder;
    }
}
