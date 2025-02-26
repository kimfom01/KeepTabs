using Asp.Versioning;
using KeepTabs.Entities;
using KeepTabs.Requests;
using KeepTabs.Services;
using MongoDB.Driver;

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

        app.MapGroup("v{v:apiVersion}/track")
            .WithApiVersionSet(apiVersionSet)
            .WithTags("Tracking")
            .MapStartTracking()
            .MapCancelTracking()
            .MapCheckTrackingStatus();
    }

    private static IEndpointRouteBuilder MapStartTracking(this IEndpointRouteBuilder group)
    {
        group.MapPost("start",
                async (TrackingRequest request, MonitorService monitorService, MongoDbProvider dbProvider) =>
                {
                    var monitorResponse = monitorService.StartMonitoring(request);

                    var jobTracking = new JobTracking
                    {
                        Id = monitorResponse.JobId,
                        JobTitle = request.Title,
                        JobUrl = request.Url,
                        RequestInterval = request.Interval,
                        ResponseStatus = new ResponseStatus
                        {
                            Id = Guid.NewGuid(),
                        }
                    };

                    await dbProvider.Collection.InsertOneAsync(jobTracking);

                    return Results.AcceptedAtRoute(TrackJobStatus, new { jobId = monitorResponse.JobId }, jobTracking);
                })
            .WithSummary("Monitor")
            .WithDescription("Endpoint to initiate a monitoring job. Returns the job ID");

        return group;
    }

    private static IEndpointRouteBuilder MapCancelTracking(this IEndpointRouteBuilder group)
    {
        group.MapPut("{jobId}/cancel",
                async (string jobId, MonitorService monitorService, MongoDbProvider dbProvider) =>
                {
                    monitorService.CancelMonitoring(jobId);

                    var jobTracking = await dbProvider.Collection.Find(x => x.Id == jobId).FirstOrDefaultAsync();

                    jobTracking.ResponseStatus.RunningState = RunningState.Down;
                    jobTracking.ResponseStatus.RunningStateName = nameof(RunningState.Down);

                    await dbProvider.Collection.ReplaceOneAsync(x => x.Id == jobId, jobTracking);

                    return Results.Ok("Success");
                })
            .WithSummary("Cancel")
            .WithDescription("Endpoint to cancel an existing monitoring job.");

        return group;
    }

    private static IEndpointRouteBuilder MapCheckTrackingStatus(this IEndpointRouteBuilder group)
    {
        group.MapGet("{jobId}/status",
                async (string jobId, MongoDbProvider dbProvider) =>
                {
                    var jobTracking = await dbProvider.Collection.Find(x => x.Id == jobId).FirstOrDefaultAsync();

                    if (jobTracking is null)
                    {
                        return Results.NotFound();
                    }

                    return Results.Ok(jobTracking);
                })
            .WithName(TrackJobStatus)
            .WithSummary("Check Job Status")
            .WithDescription("Endpoint to check status of existing job. Returns the job tracking object");

        return group;
    }
}