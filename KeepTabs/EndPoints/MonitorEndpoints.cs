using Asp.Versioning;

namespace KeepTabs.EndPoints;

public static class MonitorEndpoints
{
    public static void MapMonitorEndpoints(this WebApplication app)
    {
        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        app.MapGroup("v{v:apiVersion}/monitor")
            .WithApiVersionSet(apiVersionSet)
            .WithTags("Monitoring");
    }
}