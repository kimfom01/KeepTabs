using KeepTabs.Application.Common.Interfaces;
using KeepTabs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KeepTabs.Application.Alerts;

/// <summary>
/// Evaluates a finished monitor check against its enabled alert rules, dispatches
/// deliveries, and records logs. Runs inside the check's unit of work; it never saves.
/// </summary>
public sealed class AlertEvaluator : IAlertEvaluator
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAlertDispatcher _dispatcher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AlertEvaluator> _logger;

    public AlertEvaluator(
        IApplicationDbContext dbContext,
        IAlertDispatcher dispatcher,
        TimeProvider timeProvider,
        ILogger<AlertEvaluator> logger)
    {
        _dbContext = dbContext;
        _dispatcher = dispatcher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task EvaluateAsync(
        Domain.Monitor monitor,
        bool? previousStatusUp,
        bool currentIsUp,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var rules = await _dbContext.AlertRules
            .Where(rule => rule.MonitorId == monitor.Id && rule.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            if (IsCoolingDown(rule, now))
            {
                continue;
            }

            var (fires, message) = await ShouldFireAsync(monitor, rule, previousStatusUp, currentIsUp, cancellationToken);
            if (!fires || message is null)
            {
                continue;
            }

            AlertDeliveryResult result;
            try
            {
                result = await _dispatcher.DispatchAsync(rule, monitor, currentIsUp, message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alert dispatch failed for rule {AlertRuleId}.", rule.Id);
                result = new AlertDeliveryResult(false, ex.Message);
            }

            rule.LastFiredAt = now;
            _dbContext.AlertLogs.Add(new AlertLog
            {
                Id = Guid.CreateVersion7(),
                AlertRuleId = rule.Id,
                FiredAt = now,
                Message = message,
                Success = result.Success,
                Error = result.Error,
            });
        }
    }

    private static bool IsCoolingDown(AlertRule rule, DateTimeOffset now)
    {
        return rule.LastFiredAt is not null
            && now - rule.LastFiredAt.Value < TimeSpan.FromMinutes(rule.CoolDownMinutes);
    }

    private async Task<(bool Fires, string? Message)> ShouldFireAsync(
        Domain.Monitor monitor,
        AlertRule rule,
        bool? previousStatusUp,
        bool currentIsUp,
        CancellationToken cancellationToken)
    {
        switch (rule.TriggerType)
        {
            case AlertTriggerType.OnDown when previousStatusUp != false && !currentIsUp:
                return (true, $"Monitor '{monitor.Name}' is DOWN ({monitor.Url}).");
            case AlertTriggerType.OnUp when previousStatusUp == false && currentIsUp:
                return (true, $"Monitor '{monitor.Name}' is back UP ({monitor.Url}).");
            case AlertTriggerType.ConsecutiveFailures when !currentIsUp:
            {
                // The current check is not persisted yet, so the threshold counts it
                // plus the preceding persisted failures.
                var needed = Math.Max(0, rule.Threshold - 1);
                var recent = await _dbContext.MonitorChecks
                    .AsNoTracking()
                    .Where(check => check.MonitorId == monitor.Id)
                    .OrderByDescending(check => check.Timestamp)
                    .Take(needed)
                    .ToListAsync(cancellationToken);
                if (recent.Count == needed && recent.All(check => !check.IsUp))
                {
                    return (true, $"Monitor '{monitor.Name}' failed {rule.Threshold} checks in a row ({monitor.Url}).");
                }

                return (false, null);
            }
            default:
                return (false, null);
        }
    }
}
