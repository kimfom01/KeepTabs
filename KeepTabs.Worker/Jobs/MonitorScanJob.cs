using KeepTabs.Application.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace KeepTabs.Worker.Jobs;

/// <summary>
/// Finds due monitors and dispatches one durable check job per monitor.
/// </summary>
[DisallowConcurrentExecution]
public sealed class MonitorScanJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonitorScanJob> _logger;

    public MonitorScanJob(IServiceScopeFactory scopeFactory, ILogger<MonitorScanJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMonitorCheckRunner>();
        var monitorIds = await runner.GetDueMonitorIdsAsync(context.CancellationToken);

        _logger.LogInformation("Dispatching {MonitorCount} due monitor checks.", monitorIds.Count);

        foreach (var monitorId in monitorIds)
        {
            var data = new JobDataMap
            {
                { MonitorCheckJob.MonitorIdKey, monitorId.ToString() }
            };

            await context.Scheduler.TriggerJob(
                new JobKey(MonitorCheckJob.JobName),
                data,
                context.CancellationToken);
        }
    }
}
