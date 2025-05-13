using System.Diagnostics;
using KeepTabs.Contracts.Requests;
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
            .Include(x => x.ResponseStatus)
            .SingleAsync(x => x.Id == job.JobId, cancellationToken: cancellationToken);

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();

            var timestamp = Stopwatch.GetTimestamp();

            var response = await httpClient.GetAsync(job.Url, cancellationToken);

            var elapsed = Stopwatch.GetElapsedTime(timestamp);

            response.EnsureSuccessStatusCode();

            jobTracking.ResponseStatus.RunningState = RunningState.Up;
            jobTracking.ResponseStatus.StatusMessage = response.ReasonPhrase;
            jobTracking.ResponseStatus.ResponseLatency = elapsed.TotalMilliseconds;
            jobTracking.ResponseStatus.StatusCode = (int)response.StatusCode;

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error Processing Job {@Exception}", ex);
            jobTracking.ResponseStatus.RunningState = RunningState.Down;
            jobTracking.ResponseStatus.StatusMessage = ex.Message;
            jobTracking.ResponseStatus.StatusCode = (int)ex.StatusCode!;

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Finished Processing Job {@Request}", job);
    }
}