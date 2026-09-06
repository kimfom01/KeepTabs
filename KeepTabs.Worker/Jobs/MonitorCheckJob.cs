using KeepTabs.Application.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace KeepTabs.Worker.Jobs;

/// <summary>
/// Executes and persists one monitor check.
/// </summary>
public sealed class MonitorCheckJob : IJob
{
    public const string JobName = "monitor-check-job";
    public const string MonitorIdKey = "monitorId";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonitorCheckJob> _logger;

    public MonitorCheckJob(IServiceScopeFactory scopeFactory, ILogger<MonitorCheckJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var rawMonitorId = context.MergedJobDataMap.GetString(MonitorIdKey);
        if (!Guid.TryParse(rawMonitorId, out var monitorId))
        {
            _logger.LogWarning("Ignoring monitor check without a valid monitor ID.");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMonitorCheckRunner>();

        await runner.RunCheckAsync(monitorId, context.CancellationToken);
    }
}
