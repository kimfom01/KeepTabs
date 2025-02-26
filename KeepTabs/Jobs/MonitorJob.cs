using System.Diagnostics;
using KeepTabs.Entities;
using KeepTabs.Requests;
using KeepTabs.Services;
using MongoDB.Driver;

namespace KeepTabs.Jobs;

public class MonitorJob
{
    private readonly ILogger<MonitorJob> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMongoCollection<JobTracking> _collection;

    public MonitorJob(ILogger<MonitorJob> logger, IHttpClientFactory httpClientFactory, MongoDbProvider mongoDbProvider)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _collection = mongoDbProvider.Collection;
    }

    public async Task RunMonitoring(TrackingJob job)
    {
        _logger.LogInformation("Start Processing Job {@Request}", job);

        var jobTracking = await _collection.Find(x => x.Id == job.JobId).FirstOrDefaultAsync();

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();

            var timestamp = Stopwatch.GetTimestamp();

            var response = await httpClient.GetAsync(job.Url);

            var elapsed = Stopwatch.GetElapsedTime(timestamp);

            response.EnsureSuccessStatusCode();

            jobTracking.ResponseStatus.RunningState = RunningState.Up;
            jobTracking.ResponseStatus.StatusMessage = response.ReasonPhrase;
            jobTracking.ResponseStatus.ResponseLatency = elapsed.TotalMilliseconds;
            jobTracking.ResponseStatus.StatusCode = (int)response.StatusCode;

            await _collection.ReplaceOneAsync(x => x.Id == job.JobId, jobTracking);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error Processing Job {@Exception}", ex);
            jobTracking.ResponseStatus.RunningState = RunningState.Down;
            jobTracking.ResponseStatus.StatusMessage = ex.Message;
            jobTracking.ResponseStatus.StatusCode = (int)ex.StatusCode;

            await _collection.ReplaceOneAsync(x => x.Id == job.JobId, jobTracking);
        }

        _logger.LogInformation("Finished Processing Job {@Request}", job);
    }
}