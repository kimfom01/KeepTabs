using Hangfire;
using KeepTabs.Jobs;
using KeepTabs.Requests;
using KeepTabs.Responses;

namespace KeepTabs.Services;

public class MonitorService
{
    public MonitorResponse StartMonitoring(TrackingRequest request)
    {
        var jobId = CreateJobId(request.Title);

        var cronExpression = ConvertToCronExpression(request.Interval);

        RecurringJob.AddOrUpdate<MonitorJob>(jobId, x => x.InitiateMonitoring(request), cronExpression);

        return new MonitorResponse(jobId);
    }

    public void CancelMonitoring(string jobId)
    {
        RecurringJob.RemoveIfExists(jobId);
    }
    
    private string ConvertToCronExpression(int interval)
    {
        string cronExpression;
        if (interval is <= 0 or >= 60)
        {
            cronExpression = Cron.Minutely();
        }
        else
        {
            cronExpression = $"*/{interval} * * * *";
        }

        return cronExpression;
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