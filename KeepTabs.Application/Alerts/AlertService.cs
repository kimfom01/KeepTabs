using FluentValidation;
using KeepTabs.Application.Alerts.Dtos;
using KeepTabs.Application.Common.Interfaces;
using KeepTabs.Domain;
using Microsoft.EntityFrameworkCore;

namespace KeepTabs.Application.Alerts;

/// <summary>
/// Alert-rule use cases scoped to the authenticated owner through the owning monitor.
/// </summary>
public sealed class AlertService : IAlertService
{
    private const int MaxLogs = 100;

    private readonly IApplicationDbContext _dbContext;
    private readonly IValidator<CreateAlertRuleRequest> _createValidator;
    private readonly IAlertDispatcher _dispatcher;
    private readonly TimeProvider _timeProvider;

    public AlertService(
        IApplicationDbContext dbContext,
        IValidator<CreateAlertRuleRequest> createValidator,
        IAlertDispatcher dispatcher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _createValidator = createValidator;
        _dispatcher = dispatcher;
        _timeProvider = timeProvider;
    }

    public async Task<GetAlertRuleResponse> CreateAsync(
        string userId,
        CreateAlertRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var monitor = await OwnedMonitorAsync(userId, request.MonitorId, cancellationToken);
        if (monitor is null)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("MonitorId", "Unknown monitor.")]);
        }

        var rule = AlertMappings.ToEntity(request);
        _dbContext.AlertRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return rule.ToResponse(monitor.Name);
    }

    public async Task<IReadOnlyList<GetAlertRuleResponse>> ListAsync(
        string userId,
        Guid? monitorId,
        CancellationToken cancellationToken = default)
    {
        return await (from rule in _dbContext.AlertRules.AsNoTracking()
                      join monitor in _dbContext.Monitors.AsNoTracking() on rule.MonitorId equals monitor.Id
                      where monitor.UserId == userId && (monitorId == null || rule.MonitorId == monitorId)
                      orderby monitor.Name, rule.Id
                      select new GetAlertRuleResponse(
                          rule.Id,
                          rule.MonitorId,
                          monitor.Name,
                          rule.Type,
                          rule.TriggerType,
                          rule.Threshold,
                          rule.CoolDownMinutes,
                          rule.IsEnabled,
                          rule.Target,
                          rule.LastFiredAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<GetAlertRuleResponse?> GetByIdAsync(
        string userId,
        Guid alertRuleId,
        CancellationToken cancellationToken = default)
    {
        var rule = await FindOwnedAsync(userId, alertRuleId, cancellationToken, tracked: false);

        return rule?.ToResponse(rule.Monitor.Name);
    }

    public async Task<GetAlertRuleResponse?> UpdateAsync(
        string userId,
        Guid alertRuleId,
        UpdateAlertRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var found = await FindOwnedAsync(userId, alertRuleId, cancellationToken, tracked: true);
        if (found is null)
        {
            return null;
        }

        if (request.Type is null
            && request.TriggerType is null
            && request.Threshold is null
            && request.CoolDownMinutes is null
            && request.Target is null
            && request.IsEnabled is null)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("request", "At least one alert field must be supplied.")]);
        }

        var effective = new CreateAlertRuleRequest(
            found.MonitorId,
            request.Type ?? found.Type,
            request.TriggerType ?? found.TriggerType,
            request.Threshold ?? found.Threshold,
            request.CoolDownMinutes ?? found.CoolDownMinutes,
            request.Target?.Trim() ?? found.Target,
            request.IsEnabled ?? found.IsEnabled);
        var validation = await _createValidator.ValidateAsync(effective, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        found.Type = effective.Type;
        found.TriggerType = effective.TriggerType;
        found.Threshold = effective.Threshold;
        found.CoolDownMinutes = effective.CoolDownMinutes;
        found.Target = effective.Target;
        found.IsEnabled = effective.IsEnabled;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return found.ToResponse(found.Monitor.Name);
    }

    public async Task<bool> DeleteAsync(
        string userId,
        Guid alertRuleId,
        CancellationToken cancellationToken = default)
    {
        var found = await FindOwnedAsync(userId, alertRuleId, cancellationToken, tracked: true);
        if (found is null)
        {
            return false;
        }

        _dbContext.AlertRules.Remove(found);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<AlertLogResponse>> GetLogsAsync(
        string userId,
        Guid? monitorId,
        CancellationToken cancellationToken = default)
    {
        return await (from log in _dbContext.AlertLogs.AsNoTracking()
                      join rule in _dbContext.AlertRules.AsNoTracking() on log.AlertRuleId equals rule.Id
                      join monitor in _dbContext.Monitors.AsNoTracking() on rule.MonitorId equals monitor.Id
                      where monitor.UserId == userId && (monitorId == null || rule.MonitorId == monitorId)
                      orderby log.FiredAt descending
                      select new AlertLogResponse(
                          log.Id,
                          log.AlertRuleId,
                          rule.MonitorId,
                          monitor.Name,
                          log.FiredAt,
                          log.Message,
                          log.Success,
                          log.Error))
            .Take(MaxLogs)
            .ToListAsync(cancellationToken);
    }

    public async Task<TestAlertResponse?> TestAsync(
        string userId,
        Guid alertRuleId,
        CancellationToken cancellationToken = default)
    {
        var found = await FindOwnedAsync(userId, alertRuleId, cancellationToken, tracked: true);
        if (found is null)
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        var message = $"Test alert for monitor '{found.Monitor.Name}'.";
        var result = await _dispatcher.DispatchAsync(
            found,
            found.Monitor,
            found.Monitor.LastStatusUp ?? true,
            message,
            cancellationToken);

        _dbContext.AlertLogs.Add(new AlertLog
        {
            Id = Guid.CreateVersion7(),
            AlertRuleId = found.Id,
            FiredAt = now,
            Message = message,
            Success = result.Success,
            Error = result.Error,
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TestAlertResponse(result.Success, result.Error, message);
    }

    private async Task<Domain.Monitor?> OwnedMonitorAsync(
        string userId, Guid monitorId, CancellationToken cancellationToken)
    {
        return await _dbContext.Monitors
            .FirstOrDefaultAsync(monitor => monitor.Id == monitorId && monitor.UserId == userId, cancellationToken);
    }

    private async Task<AlertRule?> FindOwnedAsync(
        string userId, Guid alertRuleId, CancellationToken cancellationToken, bool tracked)
    {
        var query = _dbContext.AlertRules
            .Include(rule => rule.Monitor)
            .Where(rule => rule.Id == alertRuleId && rule.Monitor.UserId == userId);
        if (!tracked)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }
}
