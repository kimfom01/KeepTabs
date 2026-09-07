using KeepTabs.Application.Common.Interfaces;
using KeepTabs.Domain;
using Microsoft.EntityFrameworkCore;

namespace KeepTabs.Application.Monitoring;

/// <summary>
/// Aggregates raw checks into per-monitor daily summaries for fast range queries.
/// </summary>
public sealed class DailyUptimeAggregator : IUptimeAggregator
{
    private readonly IApplicationDbContext _dbContext;

    public DailyUptimeAggregator(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> AggregateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var start = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = start.AddDays(1);

        var aggregates = await _dbContext.MonitorChecks
            .AsNoTracking()
            .Where(check => check.Timestamp >= start && check.Timestamp < end)
            .GroupBy(check => check.MonitorId)
            .Select(group => new
            {
                MonitorId = group.Key,
                TotalChecks = group.Count(),
                UpCount = group.Count(check => check.IsUp),
                AverageResponseTimeMs = group.Average(check => (double)check.ResponseTimeMs),
            })
            .ToListAsync(cancellationToken);

        var existing = await _dbContext.DailyUptimeSummaries
            .Where(summary => summary.Date == date)
            .ToListAsync(cancellationToken);
        _dbContext.DailyUptimeSummaries.RemoveRange(existing);

        foreach (var aggregate in aggregates)
        {
            _dbContext.DailyUptimeSummaries.Add(new DailyUptimeSummary
            {
                MonitorId = aggregate.MonitorId,
                Date = date,
                TotalChecks = aggregate.TotalChecks,
                UpCount = aggregate.UpCount,
                AverageResponseTimeMs = aggregate.AverageResponseTimeMs,
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return aggregates.Count;
    }

    public async Task<int> AggregateHourlyAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        var start = TruncateToHour(from);
        var end = TruncateToHour(to);
        if (end <= start)
        {
            return 0;
        }

        var aggregates = await _dbContext.MonitorChecks
            .AsNoTracking()
            .Where(check => check.Timestamp >= start && check.Timestamp < end)
            .GroupBy(check => new
            {
                check.MonitorId,
                check.Timestamp.Year,
                check.Timestamp.Month,
                check.Timestamp.Day,
                check.Timestamp.Hour,
            })
            .Select(group => new
            {
                group.Key.MonitorId,
                Hour = new DateTimeOffset(group.Key.Year, group.Key.Month, group.Key.Day, group.Key.Hour, 0, 0, TimeSpan.Zero),
                TotalChecks = group.Count(),
                UpCount = group.Count(check => check.IsUp),
                AverageResponseTimeMs = group.Average(check => (double)check.ResponseTimeMs),
            })
            .ToListAsync(cancellationToken);

        var existing = await _dbContext.HourlyUptimeSummaries
            .Where(summary => summary.Hour >= start && summary.Hour < end)
            .ToListAsync(cancellationToken);
        _dbContext.HourlyUptimeSummaries.RemoveRange(existing);

        foreach (var aggregate in aggregates)
        {
            _dbContext.HourlyUptimeSummaries.Add(new HourlyUptimeSummary
            {
                MonitorId = aggregate.MonitorId,
                Hour = aggregate.Hour,
                TotalChecks = aggregate.TotalChecks,
                UpCount = aggregate.UpCount,
                AverageResponseTimeMs = aggregate.AverageResponseTimeMs,
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return aggregates.Count;
    }

    private static DateTimeOffset TruncateToHour(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, 0, 0, TimeSpan.Zero);
    }
}
