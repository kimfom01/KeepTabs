using KeepTabs.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DomainMonitor = KeepTabs.Domain.Monitor;

namespace KeepTabs.Application.Monitoring;

/// <summary>
/// Loads due monitors, executes protocol probes, and persists check results in one unit of work.
/// </summary>
public sealed class MonitorCheckRunner : IMonitorCheckRunner
{
    private const int MaxDueChecksPerScan = 100;
    private const int MaxErrorMessageLength = 2_000;

    private readonly IApplicationDbContext _dbContext;
    private readonly IEnumerable<IMonitorProbe> _probes;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MonitorCheckRunner> _logger;

    public MonitorCheckRunner(
        IApplicationDbContext dbContext,
        IEnumerable<IMonitorProbe> probes,
        TimeProvider timeProvider,
        ILogger<MonitorCheckRunner> logger)
    {
        _dbContext = dbContext;
        _probes = probes;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Guid>> GetDueMonitorIdsAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        return await _dbContext.Monitors
            .AsNoTracking()
            .Where(monitor => !monitor.IsPaused
                && (monitor.LastCheckedAt == null || monitor.LastCheckedAt <= now.AddSeconds(-monitor.CheckIntervalSeconds)))
            .OrderBy(monitor => monitor.LastCheckedAt)
            .Take(MaxDueChecksPerScan)
            .Select(monitor => monitor.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> RunDueChecksAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var monitors = await _dbContext.Monitors
            .Where(monitor => !monitor.IsPaused
                && (monitor.LastCheckedAt == null || monitor.LastCheckedAt <= now.AddSeconds(-monitor.CheckIntervalSeconds)))
            .OrderBy(monitor => monitor.LastCheckedAt)
            .Take(MaxDueChecksPerScan)
            .ToListAsync(cancellationToken);
        var completed = 0;

        foreach (var monitor in monitors)
        {
            if (await RunCheckAsync(monitor, cancellationToken))
            {
                completed++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return completed;
    }

    public async Task<bool> RunCheckAsync(Guid monitorId, CancellationToken cancellationToken = default)
    {
        var monitor = await _dbContext.Monitors.FirstOrDefaultAsync(monitor => monitor.Id == monitorId, cancellationToken);
        if (monitor is null || monitor.IsPaused)
        {
            return false;
        }

        var completed = await RunCheckAsync(monitor, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // The monitor was modified or deleted by another process (an API request
            // or a second worker instance) after it was loaded, so the check result
            // can no longer be persisted. Drop it instead of failing the job; the
            // next scan observes the current state.
            _logger.LogWarning(
                ex,
                "Monitor check for monitor {MonitorId} was not persisted because the monitor changed concurrently.",
                monitorId);

            return false;
        }

        return completed;
    }

    private async Task<bool> RunCheckAsync(DomainMonitor monitor, CancellationToken cancellationToken)
    {
        var probe = _probes.SingleOrDefault(candidate => candidate.CanHandle(monitor.Protocol));
        if (probe is null)
        {
            _logger.LogError("No probe is registered for protocol {Protocol} on monitor {MonitorId}.", monitor.Protocol, monitor.Id);
            var unsupported = monitor.RecordProbeResult(false, null, 0, $"Unsupported protocol: {monitor.Protocol}.", _timeProvider.GetUtcNow());
            _dbContext.MonitorChecks.Add(unsupported);

            return false;
        }

        try
        {
            var result = await probe.CheckAsync(monitor, cancellationToken);
            var check = monitor.RecordProbeResult(
                result.IsUp,
                result.StatusCode,
                result.ResponseTimeMs,
                Truncate(result.ErrorMessage),
                _timeProvider.GetUtcNow());
            _dbContext.MonitorChecks.Add(check);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Monitor check failed for monitor {MonitorId}.", monitor.Id);
            monitor.RecordProbeResult(false, null, 0, Truncate(ex.Message), _timeProvider.GetUtcNow());

            return false;
        }
    }

    private static string? Truncate(string? value)
    {
        if (value is null || value.Length <= MaxErrorMessageLength)
        {
            return value;
        }

        return value[..MaxErrorMessageLength];
    }
}
