using KeepTabs.Application.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace KeepTabs.Worker.Jobs;

/// <summary>
/// Aggregates yesterday's checks into daily summaries once per day.
/// Idempotent: re-running a date replaces its rows.
/// </summary>
[DisallowConcurrentExecution]
public sealed class DailySummaryJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailySummaryJob> _logger;
    private readonly TimeProvider _timeProvider;

    public DailySummaryJob(
        IServiceScopeFactory scopeFactory,
        ILogger<DailySummaryJob> logger,
        TimeProvider timeProvider)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var now = _timeProvider.GetUtcNow();
        var yesterday = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1);

        using var scope = _scopeFactory.CreateScope();
        var aggregator = scope.ServiceProvider.GetRequiredService<IUptimeAggregator>();
        var monitors = await aggregator.AggregateAsync(yesterday, context.CancellationToken);

        var hour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);
        var hours = await aggregator.AggregateHourlyAsync(hour.AddDays(-3), hour, context.CancellationToken);

        _logger.LogInformation(
            "Aggregated daily summaries for {Date} across {MonitorCount} monitors ({HourBuckets} hourly buckets).",
            yesterday, monitors, hours);
    }
}
