using System.Diagnostics;
using KeepTabs.Database;
using KeepTabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace KeepTabs.Jobs;

public class MonitorJobService
{
    private readonly ILogger<MonitorJobService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeepTabsDbContext _context;

    public MonitorJobService(ILogger<MonitorJobService> logger, IHttpClientFactory httpClientFactory, KeepTabsDbContext context)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    public async Task RunMonitoring(MonitorJob monitorJob, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start Processing Job {@Request}", monitorJob);

        var monitor = await _context.Monitors
            .Include(x => x.ResponseStatuses)
            .SingleAsync(x => x.Id == monitorJob.MonitorId, cancellationToken: cancellationToken);

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();

            var timestamp = Stopwatch.GetTimestamp();

            var response = await httpClient.GetAsync(monitorJob.Url, cancellationToken);

            var elapsed = Stopwatch.GetElapsedTime(timestamp);

            response.EnsureSuccessStatusCode();

            var responseStatus = new ResponseStatus
            {
                Id = Guid.NewGuid(),
                StatusCode = (int)response.StatusCode,
                ResponseLatency = elapsed.TotalMilliseconds,
                StatusMessage = response.ReasonPhrase,
                RunningState = RunningState.Up,
                MonitorId = monitor.Id,
            };

            await _context.ResponseStatuses.AddAsync(responseStatus, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error Processing Job {@Exception}", ex);
            var responseStatus = new ResponseStatus
            {
                Id = Guid.NewGuid(),
                StatusCode = (int)ex.StatusCode!,
                StatusMessage = ex.Message,
                RunningState = RunningState.Down,
                MonitorId = monitor.Id,
            };

            await _context.ResponseStatuses.AddAsync(responseStatus, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Finished Processing Job {@Request}", monitorJob);
    }
}