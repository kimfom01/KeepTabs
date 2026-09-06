using System.Reflection;
using FluentValidation;
using KeepTabs.Application.Monitors;
using KeepTabs.Application.Monitoring;
using KeepTabs.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace KeepTabs.Application;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IMonitorService, MonitorService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddMonitorChecking();
    }

    public static void AddMonitorChecking(this IServiceCollection services)
    {
        services.AddScoped<IMonitorCheckRunner, MonitorCheckRunner>();
    }
}

