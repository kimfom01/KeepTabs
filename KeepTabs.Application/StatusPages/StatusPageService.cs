using FluentValidation;
using KeepTabs.Application.Common.Interfaces;
using KeepTabs.Application.Monitors.Dtos;
using KeepTabs.Application.StatusPages.Dtos;
using KeepTabs.Domain;
using Microsoft.EntityFrameworkCore;

namespace KeepTabs.Application.StatusPages;

/// <summary>
/// Status-page use cases scoped to the authenticated owner, plus anonymous reads.
/// </summary>
public sealed class StatusPageService : IStatusPageService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IValidator<CreateStatusPageRequest> _createValidator;
    private readonly TimeProvider _timeProvider;

    public StatusPageService(
        IApplicationDbContext dbContext,
        IValidator<CreateStatusPageRequest> createValidator,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _createValidator = createValidator;
        _timeProvider = timeProvider;
    }

    public async Task<GetStatusPageResponse> CreateAsync(
        string userId,
        CreateStatusPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var slug = await UniqueSlugAsync(
            Slugify(request.Slug, request.Name), null, cancellationToken);

        var monitors = await OwnedMonitorsAsync(userId, request.MonitorIds, cancellationToken);
        if (monitors.Count != request.MonitorIds.Distinct().Count())
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("MonitorIds", "Unknown monitor.")]);
        }

        var page = new StatusPage
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = request.Name.Trim(),
            Slug = slug,
            IsPublic = request.IsPublic,
            Monitors = monitors
                .Select<Domain.Monitor, StatusPageMonitor>((monitor, index) => new StatusPageMonitor
                {
                    MonitorId = monitor.Id,
                    SortOrder = index,
                })
                .ToList(),
        };

        _dbContext.StatusPages.Add(page);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(page, monitors.ToDictionary(monitor => monitor.Id, monitor => monitor.Name));
    }

    public async Task<IReadOnlyList<GetStatusPageResponse>> ListAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var pages = await _dbContext.StatusPages
            .AsNoTracking()
            .Include(page => page.Monitors)
            .ThenInclude(link => link.Monitor)
            .Where(page => page.UserId == userId)
            .OrderBy(page => page.Name)
            .ToListAsync(cancellationToken);

        return pages.Select(page => ToResponse(page)).ToList();
    }

    public async Task<GetStatusPageResponse?> GetByIdAsync(
        string userId,
        Guid statusPageId,
        CancellationToken cancellationToken = default)
    {
        var page = await FindOwnedAsync(userId, statusPageId, cancellationToken, tracked: false);

        return page is null ? null : ToResponse(page);
    }

    public async Task<GetStatusPageResponse?> UpdateAsync(
        string userId,
        Guid statusPageId,
        UpdateStatusPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = await FindOwnedAsync(userId, statusPageId, cancellationToken, tracked: true);
        if (page is null)
        {
            return null;
        }

        if (request.Name is null && request.Slug is null && request.IsPublic is null && request.MonitorIds is null)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("request", "At least one status page field must be supplied.")]);
        }

        string slug;
        if (request.Slug is null)
        {
            // Untouched: renaming alone keeps the published link stable.
            slug = page.Slug;
        }
        else
        {
            var effectiveName = request.Name?.Trim() ?? page.Name;
            slug = await UniqueSlugAsync(Slugify(request.Slug, effectiveName), page.Id, cancellationToken);
        }

        List<Domain.Monitor>? monitors = null;
        if (request.MonitorIds is not null)
        {
            monitors = await OwnedMonitorsAsync(userId, request.MonitorIds, cancellationToken);
            if (monitors.Count != request.MonitorIds.Distinct().Count())
            {
                throw new ValidationException([new FluentValidation.Results.ValidationFailure("MonitorIds", "Unknown monitor.")]);
            }
        }

        var effective = new CreateStatusPageRequest(
            request.Name?.Trim() ?? page.Name,
            slug,
            request.IsPublic ?? page.IsPublic,
            monitors?.Select(monitor => monitor.Id).ToList() ?? page.Monitors.Select(link => link.MonitorId).ToList());
        var validation = await _createValidator.ValidateAsync(effective, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        page.Name = effective.Name;
        page.Slug = effective.Slug ?? page.Slug;
        page.IsPublic = effective.IsPublic;

        if (monitors is not null)
        {
            _dbContext.StatusPageMonitors.RemoveRange(page.Monitors);
            page.Monitors = monitors
                .Select((monitor, index) => new StatusPageMonitor
                {
                    StatusPageId = page.Id,
                    MonitorId = monitor.Id,
                    SortOrder = index,
                })
                .ToList();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(page);
    }

    public async Task<bool> DeleteAsync(
        string userId,
        Guid statusPageId,
        CancellationToken cancellationToken = default)
    {
        var page = await FindOwnedAsync(userId, statusPageId, cancellationToken, tracked: true);
        if (page is null)
        {
            return false;
        }

        _dbContext.StatusPages.Remove(page);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<PublicStatusPageResponse?> GetPublicAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var page = await _dbContext.StatusPages
            .AsNoTracking()
            .Include(page => page.Monitors)
            .ThenInclude(link => link.Monitor)
            .FirstOrDefaultAsync(page => page.Slug == normalized && page.IsPublic, cancellationToken);
        if (page is null)
        {
            return null;
        }

        var monitorIds = page.Monitors.Select(link => link.MonitorId).ToList();
        var stats = await _dbContext.MonitorChecks
            .AsNoTracking()
            .Where(check => monitorIds.Contains(check.MonitorId))
            .GroupBy(check => check.MonitorId)
            .Select(group => new
            {
                MonitorId = group.Key,
                TotalChecks = group.Count(),
                UpCount = group.Count(check => check.IsUp),
            })
            .ToListAsync(cancellationToken);
        var byMonitor = stats.ToDictionary(stat => stat.MonitorId);

        var now = _timeProvider.GetUtcNow();
        var hourCutoff = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero).AddHours(-48);
        var hourlyRows = await _dbContext.HourlyUptimeSummaries
            .AsNoTracking()
            .Where(summary => monitorIds.Contains(summary.MonitorId) && summary.Hour >= hourCutoff)
            .OrderBy(summary => summary.Hour)
            .ToListAsync(cancellationToken);
        var dayCutoff = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-30);
        var dailyRows = await _dbContext.DailyUptimeSummaries
            .AsNoTracking()
            .Where(summary => monitorIds.Contains(summary.MonitorId) && summary.Date > dayCutoff)
            .OrderBy(summary => summary.Date)
            .ToListAsync(cancellationToken);

        return new PublicStatusPageResponse(
            page.Name,
            page.Slug,
            page.Monitors
                .OrderBy(link => link.SortOrder)
                .Select(link => new PublicStatusMonitor(
                    link.MonitorId,
                    link.Monitor.Name,
                    link.Monitor.Url,
                    link.Monitor.Protocol,
                    link.Monitor.LastStatusUp,
                    link.Monitor.LastCheckedAt,
                    byMonitor.TryGetValue(link.MonitorId, out var stat) && stat.TotalChecks > 0
                        ? (double)stat.UpCount / stat.TotalChecks * 100
                        : null,
                    hourlyRows
                        .Where(row => row.MonitorId == link.MonitorId)
                        .Select(row => new HourlyUptimeItem(
                            row.Hour,
                            row.TotalChecks,
                            row.UpCount,
                            row.TotalChecks == 0 ? 0 : (double)row.UpCount / row.TotalChecks * 100,
                            row.AverageResponseTimeMs))
                        .ToList(),
                    dailyRows
                        .Where(row => row.MonitorId == link.MonitorId)
                        .Select(row => new DailyUptimeItem(
                            row.Date,
                            row.TotalChecks,
                            row.UpCount,
                            row.TotalChecks == 0 ? 0 : (double)row.UpCount / row.TotalChecks * 100,
                            row.AverageResponseTimeMs))
                        .ToList()))
                .ToList());
    }

    public async Task<SlugAvailabilityResponse> CheckSlugAsync(
        string? slug,
        string? name,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Slugify(slug, name ?? string.Empty);
        var suggestion = await UniqueSlugAsync(normalized, excludeId, cancellationToken);

        return new SlugAvailabilityResponse(normalized, suggestion == normalized, suggestion);
    }

    private async Task<string> UniqueSlugAsync(
        string baseSlug, Guid? excludeId, CancellationToken cancellationToken)
    {
        var slug = baseSlug;
        var suffix = 2;
        while (await _dbContext.StatusPages.AnyAsync(
            page => page.Slug == slug && page.Id != excludeId, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }

    private static string Slugify(string? slug, string name)
    {
        var source = string.IsNullOrWhiteSpace(slug) ? name : slug;
        var lowered = source.Trim().ToLowerInvariant();
        var slugified = string.Concat(lowered.Select(c =>
            c is >= 'a' and <= 'z' or >= '0' and <= '9' ? c : '-'));

        while (slugified.Contains("--", StringComparison.Ordinal))
        {
            slugified = slugified.Replace("--", "-", StringComparison.Ordinal);
        }

        slugified = slugified.Trim('-');
        if (slugified.Length > 90)
        {
            slugified = slugified[..90].TrimEnd('-');
        }

        return slugified.Length == 0 ? "page" : slugified;
    }

    private async Task<List<Domain.Monitor>> OwnedMonitorsAsync(
        string userId, IReadOnlyList<Guid> monitorIds, CancellationToken cancellationToken)
    {
        var distinct = monitorIds.Distinct().ToList();
        if (distinct.Count == 0)
        {
            return [];
        }

        var monitors = await _dbContext.Monitors
            .Where(monitor => monitor.UserId == userId && distinct.Contains(monitor.Id))
            .ToListAsync(cancellationToken);
        var byId = monitors.ToDictionary(monitor => monitor.Id);

        return distinct.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
    }

    private Task<StatusPage?> FindOwnedAsync(
        string userId, Guid statusPageId, CancellationToken cancellationToken, bool tracked)
    {
        var query = _dbContext.StatusPages
            .Include(page => page.Monitors)
            .ThenInclude(link => link.Monitor)
            .Where(page => page.Id == statusPageId && page.UserId == userId);
        if (!tracked)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    private static GetStatusPageResponse ToResponse(StatusPage page, IReadOnlyDictionary<Guid, string>? names = null)
    {
        return new GetStatusPageResponse(
            page.Id,
            page.Name,
            page.Slug,
            page.IsPublic,
            page.Monitors
                .OrderBy(link => link.SortOrder)
                .Select(link => new StatusPageMonitorRef(
                    link.MonitorId,
                    names?.GetValueOrDefault(link.MonitorId) ?? link.Monitor?.Name ?? string.Empty))
                .ToList());
    }
}
