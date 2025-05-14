using Hangfire;
using KeepTabs.Contracts.Requests;
using KeepTabs.Contracts.Responses;
using KeepTabs.Entities;
using KeepTabs.Jobs;

namespace KeepTabs.Services;

public class MonitorService
{
    public MonitorResponse StartMonitoring(TrackingRequest request, CancellationToken cancellationToken)
    {
        var jobId = CreateJobId(request.Title);

        var cronExpression = ConvertToCronExpression(request.Interval);

        var job = new TrackingJob(jobId, request.Url, request.Title, request.Interval);

        RecurringJob.AddOrUpdate<MonitorJob>(jobId, x => x.RunMonitoring(job, cancellationToken), cronExpression);

        return new MonitorResponse(jobId);
    }

    public void CancelMonitoring(string jobId)
    {
        RecurringJob.RemoveIfExists(jobId);
    }

    public void CleanUpExpiredRecords(CancellationToken cancellationToken)
    {
        RecurringJob.AddOrUpdate<JobHistoryCleaner>("CleanUp", x => x.CleanUp(cancellationToken), Cron.Daily);
    }

    private string ConvertToCronExpression(int interval)
    {
        return interval switch
        {
            <= 1 or >= 60 => Cron.Minutely(),
            _ => $"*/{interval} * * * *"
        };
    }

    private string CleanUpTitle(string title)
    {
        return title.Replace(" ", "_");
    }

    private string CreateJobId(string title)
    {
        var cleanedTitle = CleanUpTitle(title);

        return $"{cleanedTitle}_{Guid.NewGuid()}";
    }
}