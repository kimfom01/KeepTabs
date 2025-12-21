using System.Reflection;
using FluentValidation;
using KeepTabs.Application.Monitors;
using Microsoft.Extensions.DependencyInjection;

namespace KeepTabs.Application;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IMonitorService, MonitorService>();
    }
}

