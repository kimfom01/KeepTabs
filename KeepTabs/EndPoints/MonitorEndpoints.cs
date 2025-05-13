using Asp.Versioning;
using KeepTabs.Contracts.Requests;
using KeepTabs.Database;
using KeepTabs.Entities;
using KeepTabs.Services;
using Microsoft.EntityFrameworkCore;

namespace KeepTabs.EndPoints;

public static class MonitorEndpoints
{
    private const string TrackJobStatus = nameof(TrackJobStatus);

    public static void MapTrackingEndpoints(this WebApplication app)
    {
        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        app.MapGroup("v{v:apiVersion}/monitor")
            .WithApiVersionSet(apiVersionSet)
            .WithTags("Monitoring")
            .MapStartTracking()
            .MapCancelTracking()
            .MapCheckTrackingStatus()
            .MapGetTrackingHistory();
    }

    private static IEndpointRouteBuilder MapStartTracking(this IEndpointRouteBuilder group)
    {
        group.MapPost("start", async (TrackingRequest request, MonitorService monitorService, KeepTabsDbContext context,
                CancellationToken cancellationToken) =>
            {
                var monitorResponse = monitorService.StartMonitoring(request, cancellationToken);

                var jobTracking = new JobTracking
                {
                    Id = monitorResponse.JobId,
                    JobTitle = request.Title,
                    JobUrl = request.Url,
                    RequestInterval = request.Interval,
                    ResponseStatuses = []
                };

                await context.JobTrackings.AddAsync(jobTracking, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                return Results.AcceptedAtRoute(TrackJobStatus, new { jobId = monitorResponse.JobId }, jobTracking);
            })
            .WithSummary("Start Monitoring")
            .WithDescription("Endpoint to initiate a monitoring job");

        return group;
    }

    private static IEndpointRouteBuilder MapCancelTracking(this IEndpointRouteBuilder group)
    {
        group.MapPut("{jobId}/cancel", async (string jobId, MonitorService monitorService, KeepTabsDbContext context,
                CancellationToken cancellationToken) =>
            {
                monitorService.CancelMonitoring(jobId);

                var jobTracking = await context.JobTrackings
                    .Include(x => x.ResponseStatuses)
                    .FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken: cancellationToken);

                if (jobTracking is null)
                {
                    return Results.NotFound("Job ID is invalid");
                }

                var responseStatus = new ResponseStatus
                {
                    Id = Guid.NewGuid(),
                    RunningState = RunningState.Down,
                    JobTrackingId = jobTracking.Id,
                };

                await context.ResponseStatuses.AddAsync(responseStatus, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                return Results.Ok($"Successfully cancelled job with ID {jobId}");
            })
            .WithSummary("Cancel Monitoring")
            .WithDescription("Endpoint to cancel an existing monitoring job");

        return group;
    }

    private static IEndpointRouteBuilder MapCheckTrackingStatus(this IEndpointRouteBuilder group)
    {
        group.MapGet("{jobId}/status", async (string jobId, KeepTabsDbContext context,
                CancellationToken cancellationToken) =>
            {
                var jobTracking = await context.JobTrackings
                    .Include(x => x.ResponseStatuses)
                    .FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);

                if (jobTracking is null)
                {
                    return Results.NotFound("Job ID is invalid");
                }

                return Results.Ok(jobTracking);
            })
            .WithName(TrackJobStatus)
            .WithSummary("Check Monitoring Status")
            .WithDescription("Endpoint to check status of existing job");

        return group;
    }

    private static IEndpointRouteBuilder MapGetTrackingHistory(this IEndpointRouteBuilder group)
    {
        group.MapGet("{jobId}/history", async (string jobId, KeepTabsDbContext context,
                CancellationToken cancellationToken) =>
            {
                var responseStatuses = await context.ResponseStatuses
                    .Where(x => x.JobTrackingId == jobId)
                    .ToListAsync(cancellationToken);

                return Results.Ok(responseStatuses);
            })
            .WithSummary("Check Monitoring History")
            .WithDescription("Endpoint to check history of existing job");

        return group;
    }
}