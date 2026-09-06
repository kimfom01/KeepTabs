using FluentValidation;
using KeepTabs.Application.Common.Interfaces;
using KeepTabs.Application.Monitors.Dtos;
using KeepTabs.Application.Users;
using KeepTabs.Domain;
using Microsoft.EntityFrameworkCore;
using DomainMonitor = KeepTabs.Domain.Monitor;

namespace KeepTabs.Application.Monitors;

/// <summary>
/// Monitor use cases scoped to the authenticated owner.
/// </summary>
public sealed class MonitorService : IMonitorService
{
    private const int MaxHistoryDays = 365;
    private const int MaxHistoryItems = 1_000;

    private readonly IApplicationDbContext _dbContext;
    private readonly IUserAccountStore _userAccounts;
    private readonly IValidator<CreateMonitorRequest> _createValidator;
    private readonly TimeProvider _timeProvider;

    public MonitorService(
        IApplicationDbContext dbContext,
        IUserAccountStore userAccounts,
        IValidator<CreateMonitorRequest> createValidator,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _userAccounts = userAccounts;
        _createValidator = createValidator;
        _timeProvider = timeProvider;
    }

    public async Task<GetMonitorResponse> CreateMonitorAsync(
        string userId,
        CreateMonitorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _userAccounts.ExistsAsync(userId, cancellationToken))
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("UserId", "Unknown user.")]);
        }

        if (await _dbContext.Monitors.AnyAsync(
                monitor => monitor.UserId == userId && monitor.Name == request.Name.Trim(),
                cancellationToken))
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("Name", $"Monitor '{request.Name}' already exists.")]);
        }

        var monitor = MonitorMappings.ToEntity(userId, request);
        await _dbContext.Monitors.AddAsync(monitor, cancellationToken);
        monitor.AddDomainEvent(new MonitorCreateEvent
        {
            MonitorId = monitor.Id,
            UserId = userId
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return monitor.ToResponse();
    }

    public async Task<GetMonitorResponse?> GetMonitorByIdAsync(
        string userId,
        Guid monitorId,
        CancellationToken cancellationToken = default)
    {
        var monitor = await FindOwnedAsync(userId, monitorId, cancellationToken, tracked: false);

        return monitor?.ToResponse();
    }

    public async Task<IReadOnlyList<GetMonitorResponse>> GetMonitorsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Monitors
            .AsNoTracking()
            .Where(monitor => monitor.UserId == userId)
            .OrderBy(monitor => monitor.Name)
            .Select(monitor => new GetMonitorResponse(
                monitor.Id,
                monitor.UserId,
                monitor.Name,
                monitor.Url,
                monitor.Protocol,
                monitor.CheckIntervalSeconds,
                monitor.TimeoutSeconds,
                monitor.ExpectedStatusCode,
                monitor.IsPaused,
                monitor.LastCheckedAt,
                monitor.LastStatusUp))
            .ToListAsync(cancellationToken);
    }

    public async Task<GetMonitorResponse?> UpdateMonitorAsync(
        string userId,
        Guid monitorId,
        UpdateMonitorRequest request,
        CancellationToken cancellationToken = default)
    {
        var monitor = await FindOwnedAsync(userId, monitorId, cancellationToken, tracked: true);
        if (monitor is null)
        {
            return null;
        }

        if (request.Name is null
            && request.Url is null
            && request.Protocol is null
            && request.CheckIntervalSeconds is null
            && request.TimeoutSeconds is null
            && request.ExpectedStatusCode is null
            && request.IsPaused is null)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("request", "At least one monitor field must be supplied.")]);
        }

        var effectiveRequest = new CreateMonitorRequest(
            request.Name?.Trim() ?? monitor.Name,
            request.Url?.Trim() ?? monitor.Url,
            request.Protocol ?? monitor.Protocol,
            request.CheckIntervalSeconds ?? monitor.CheckIntervalSeconds,
            request.TimeoutSeconds ?? monitor.TimeoutSeconds,
            request.ExpectedStatusCode ?? monitor.ExpectedStatusCode);
        var validation = await _createValidator.ValidateAsync(effectiveRequest, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        if (!string.Equals(effectiveRequest.Name, monitor.Name, StringComparison.Ordinal)
            && await _dbContext.Monitors.AnyAsync(
                other => other.UserId == userId && other.Name == effectiveRequest.Name && other.Id != monitor.Id,
                cancellationToken))
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("Name", $"Monitor '{effectiveRequest.Name}' already exists.")]);
        }

        monitor.ApplyUpdate(
            effectiveRequest.Name,
            effectiveRequest.Url,
            effectiveRequest.Protocol,
            effectiveRequest.CheckIntervalSeconds,
            effectiveRequest.TimeoutSeconds,
            effectiveRequest.ExpectedStatusCode,
            request.IsPaused);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return monitor.ToResponse();
    }

    public async Task<bool> DeleteMonitorAsync(
        string userId,
        Guid monitorId,
        CancellationToken cancellationToken = default)
    {
        var monitor = await FindOwnedAsync(userId, monitorId, cancellationToken, tracked: true);
        if (monitor is null)
        {
            return false;
        }

        _dbContext.Monitors.Remove(monitor);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<GetMonitorResponse?> SetPausedAsync(
        string userId,
        Guid monitorId,
        bool isPaused,
        CancellationToken cancellationToken = default)
    {
        var monitor = await FindOwnedAsync(userId, monitorId, cancellationToken, tracked: true);
        if (monitor is null)
        {
            return null;
        }

        monitor.SetPaused(isPaused);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return monitor.ToResponse();
    }

    public async Task<MonitorSummaryResponse?> GetSummaryAsync(
        string userId,
        Guid monitorId,
        CancellationToken cancellationToken = default)
    {
        var monitor = await FindOwnedAsync(userId, monitorId, cancellationToken, tracked: false);
        if (monitor is null)
        {
            return null;
        }

        var checks = _dbContext.MonitorChecks
            .AsNoTracking()
            .Where(check => check.MonitorId == monitorId);
        var totalChecks = await checks.CountAsync(cancellationToken);
        if (totalChecks == 0)
        {
            return new MonitorSummaryResponse(
                monitor.Id,
                monitor.Name,
                monitor.Url,
                0,
                0,
                0,
                0,
                0,
                monitor.LastCheckedAt,
                monitor.LastStatusUp);
        }

        var upCount = await checks.CountAsync(check => check.IsUp, cancellationToken);
        var averageResponseTimeMs = await checks.AverageAsync(check => (double)check.ResponseTimeMs, cancellationToken);

        return new MonitorSummaryResponse(
            monitor.Id,
            monitor.Name,
            monitor.Url,
            totalChecks,
            upCount,
            totalChecks - upCount,
            (double)upCount / totalChecks * 100,
            averageResponseTimeMs,
            monitor.LastCheckedAt,
            monitor.LastStatusUp);
    }

    public async Task<IReadOnlyList<MonitorCheckHistoryItem>> GetHistoryAsync(
        string userId,
        Guid monitorId,
        int days,
        CancellationToken cancellationToken = default)
    {
        if (await FindOwnedAsync(userId, monitorId, cancellationToken, tracked: false) is null)
        {
            return [];
        }

        var boundedDays = Math.Clamp(days, 1, MaxHistoryDays);
        var cutoff = _timeProvider.GetUtcNow().AddDays(-boundedDays);

        return await _dbContext.MonitorChecks
            .AsNoTracking()
            .Where(check => check.MonitorId == monitorId && check.Timestamp >= cutoff)
            .OrderByDescending(check => check.Timestamp)
            .Take(MaxHistoryItems)
            .Select(check => new MonitorCheckHistoryItem(
                check.Id,
                check.Timestamp,
                check.IsUp,
                check.StatusCode,
                check.ResponseTimeMs,
                check.ErrorMessage))
            .ToListAsync(cancellationToken);
    }

    private Task<DomainMonitor?> FindOwnedAsync(string userId, Guid monitorId, CancellationToken cancellationToken, bool tracked)
    {
        var query = _dbContext.Monitors.Where(monitor => monitor.Id == monitorId && monitor.UserId == userId);
        if (!tracked)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }
}
