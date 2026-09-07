namespace KeepTabs.Application.Monitoring;

/// <summary>
/// Pre-aggregates checks into daily and hourly summaries. Idempotent per bucket.
/// </summary>
public interface IUptimeAggregator
{
    Task<int> AggregateAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<int> AggregateHourlyAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}
