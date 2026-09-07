using System.Reflection;
using FluentValidation;
using KeepTabs.Application.Alerts;
using KeepTabs.Application.Monitors;
using KeepTabs.Application.Monitoring;
using KeepTabs.Application.Settings;
using KeepTabs.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace KeepTabs.Application;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IMonitorService, MonitorService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddMonitorChecking();
    }

    public static void AddMonitorChecking(this IServiceCollection services)
    {
        services.AddScoped<IMonitorCheckRunner, MonitorCheckRunner>();
        services.AddScoped<IAlertEvaluator, AlertEvaluator>();
        services.AddScoped<ISettingsService, SettingsService>();
    }
}

