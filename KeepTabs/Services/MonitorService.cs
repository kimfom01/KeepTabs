using Hangfire;
using KeepTabs.Contracts.Requests;
using KeepTabs.Contracts.Responses;
using KeepTabs.Jobs;
using MonitorJob = KeepTabs.Entities.MonitorJob;

namespace KeepTabs.Services;

public class MonitorService
{
    public MonitorResponse StartMonitoring(MonitoringRequest request, CancellationToken cancellationToken)
    {
        var monitorId = CreateMonitorId(request.Title);

        var cronExpression = ConvertToCronExpression(request.Interval);

        var job = new MonitorJob(monitorId, request.Url, request.Title, request.Interval);

        RecurringJob.AddOrUpdate<MonitorJobService>(monitorId, x => x.RunMonitoring(job, cancellationToken), cronExpression);

        return new MonitorResponse(monitorId);
    }

    public void CancelMonitoring(string monitorId)
    {
        RecurringJob.RemoveIfExists(monitorId);
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

    private string CreateMonitorId(string title)
    {
        var cleanedTitle = CleanUpTitle(title);

        return $"{cleanedTitle}_{Guid.NewGuid()}";
    }
}