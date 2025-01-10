using Asp.Versioning;
using KeepTabs.Requests;
using KeepTabs.Services;

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
            .WithTags("Monitor")
            .MapStartMonitor()
            .MapCancelMonitoring();
    }

    private static IEndpointRouteBuilder MapStartMonitor(this IEndpointRouteBuilder group)
    {
        group.MapPost("/start",
                (MonitorRequest request, MonitorService monitorService) =>
                {
                    var jobId = monitorService.StartMonitoring(request);

                    return Results.Ok(jobId);
                })
            .WithSummary("Monitor")
            .WithDescription("Endpoint to initiate a monitoring job. Returns the job ID");
        
        return group;
    }

    private static IEndpointRouteBuilder MapCancelMonitoring(this IEndpointRouteBuilder group)
    {
        group.MapGet("/cancel", (string jobId, MonitorService monitorService) =>
            {
                monitorService.CancelMonitoring(jobId);

                return Results.Ok("Success");
            })
            .WithSummary("Cancel")
            .WithDescription("Endpoint to cancel an existing monitoring job.");

        return group;
    }
}