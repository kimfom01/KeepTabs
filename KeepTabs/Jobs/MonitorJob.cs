using System.Diagnostics;
using KeepTabs.Database;
using KeepTabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace KeepTabs.Jobs;

public class MonitorJob
{
    private readonly ILogger<MonitorJob> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeepTabsDbContext _context;

    public MonitorJob(ILogger<MonitorJob> logger, IHttpClientFactory httpClientFactory, KeepTabsDbContext context)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    public async Task RunMonitoring(TrackingJob job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start Processing Job {@Request}", job);

        var jobTracking = await _context.JobTrackings
            .Include(x => x.ResponseStatuses)
            .SingleAsync(x => x.Id == job.JobId, cancellationToken: cancellationToken);

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();

            var timestamp = Stopwatch.GetTimestamp();

            var response = await httpClient.GetAsync(job.Url, cancellationToken);

            var elapsed = Stopwatch.GetElapsedTime(timestamp);

            response.EnsureSuccessStatusCode();

            var responseStatus = new ResponseStatus
            {
                Id = Guid.NewGuid(),
                StatusCode = (int)response.StatusCode,
                ResponseLatency = elapsed.TotalMilliseconds,
                StatusMessage = response.ReasonPhrase,
                RunningState = RunningState.Up,
                JobTrackingId = jobTracking.Id,
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
                JobTrackingId = jobTracking.Id,
            };

            await _context.ResponseStatuses.AddAsync(responseStatus, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Finished Processing Job {@Request}", job);
    }
}