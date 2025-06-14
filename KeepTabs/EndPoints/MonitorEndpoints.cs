using Asp.Versioning;
using KeepTabs.Contracts.Requests;
using KeepTabs.Database;
using KeepTabs.Entities;
using KeepTabs.Services;
using Microsoft.EntityFrameworkCore;
using Monitor = KeepTabs.Entities.Monitor;

namespace KeepTabs.EndPoints;

public static class MonitorEndpoints
{
    private const string MonitoringJobStatus = nameof(MonitoringJobStatus);

    public static void MapMonitorEndpoints(this WebApplication app)
    {
        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        app.MapGroup("v{v:apiVersion}/monitor")
            .WithApiVersionSet(apiVersionSet)
            .WithTags("Monitoring")
            .MapStartMonitoring()
            .MapCancelMonitoring()
            .MapCheckMonitoringStatus()
            .MapGetMonitoringHistory()
            .MapGetMonitoringEntries();
    }

    private static IEndpointRouteBuilder MapStartMonitoring(this IEndpointRouteBuilder group)
    {
        group.MapPost("start", async (MonitoringRequest request, MonitorService monitorService, KeepTabsDbContext context,
                CancellationToken cancellationToken) =>
            {
                var monitorResponse = monitorService.StartMonitoring(request, cancellationToken);

                var monitor = new Monitor
                {
                    Id = monitorResponse.MonitorId,
                    JobTitle = request.Title,
                    JobUrl = request.Url,
                    RequestInterval = request.Interval,
                    ResponseStatuses = []
                };

                await context.Monitors.AddAsync(monitor, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                return Results.AcceptedAtRoute(MonitoringJobStatus, new { monitorId = monitorResponse.MonitorId }, monitor);
            })
            .WithSummary("Start Monitoring")
            .WithDescription("Endpoint to initiate a monitoring job");

        return group;
    }

    private static IEndpointRouteBuilder MapCancelMonitoring(this IEndpointRouteBuilder group)
    {
        group.MapPut("{monitorId}/cancel", async (string monitorId, MonitorService monitorService, KeepTabsDbContext context,
                CancellationToken cancellationToken) =>
            {
                monitorService.CancelMonitoring(monitorId);

                var monitor = await context.Monitors
                    .Include(x => x.ResponseStatuses)
                    .FirstOrDefaultAsync(x => x.Id == monitorId, cancellationToken: cancellationToken);

                if (monitor is null)
                {
                    return Results.NotFound("Job ID Is Invalid. No Monitor Found!");
                }

                var responseStatus = new ResponseStatus
                {
                    Id = Guid.NewGuid(),
                    RunningState = RunningState.Down,
                    MonitorId = monitor.Id,
                };

                await context.ResponseStatuses.AddAsync(responseStatus, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                return Results.Ok($"Successfully cancelled job with ID {monitorId}");
            })
            .WithSummary("Cancel Monitoring")
            .WithDescription("Endpoint to cancel an existing monitoring job");

        return group;
    }

    private static IEndpointRouteBuilder MapCheckMonitoringStatus(this IEndpointRouteBuilder group)
    {
        group.MapGet("{monitorId}/status", async (string monitorId, KeepTabsDbContext context,
                CancellationToken cancellationToken) =>
            {
                var monitor = await context.Monitors
                    .Include(x => x.ResponseStatuses)
                    .FirstOrDefaultAsync(x => x.Id == monitorId, cancellationToken);

                if (monitor is null)
                {
                    return Results.NotFound("Job ID Is Invalid. No Monitor Found!");
                }

                return Results.Ok(monitor);
            })
            .WithName(MonitoringJobStatus)
            .WithSummary("Check Monitoring Status")
            .WithDescription("Endpoint to check status of existing job");

        return group;
    }

    private static IEndpointRouteBuilder MapGetMonitoringHistory(this IEndpointRouteBuilder group)
    {
        group.MapGet("{monitorId}/history", async (string monitorId, KeepTabsDbContext context,
                CancellationToken cancellationToken) =>
            {
                var responseStatuses = await context.ResponseStatuses
                    .Where(x => x.MonitorId == monitorId)
                    .ToListAsync(cancellationToken);

                return Results.Ok(responseStatuses);
            })
            .WithSummary("Check Monitoring History")
            .WithDescription("Endpoint to check history of existing job");

        return group;
    }

    private static IEndpointRouteBuilder MapGetMonitoringEntries(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", async (KeepTabsDbContext context, CancellationToken cancellationToken) =>
            {
                var monitors = await context.Monitors
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                return Results.Ok(monitors);
            })
            .WithSummary("Get Monitoring Entries")
            .WithDescription("Endpoint to get all monitoring entries");

        return group;
    }
}