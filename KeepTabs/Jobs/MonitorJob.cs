using KeepTabs.Requests;

namespace KeepTabs.Jobs;

public class MonitorJob
{
    private readonly ILogger<MonitorJob> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public MonitorJob(ILogger<MonitorJob> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task InitiateMonitoring(TrackingRequest request)
    {
        _logger.LogInformation("Logging Request {@Request}", request);

        using var httpClient = _httpClientFactory.CreateClient();

        var response = await httpClient.GetAsync(request.Url);

        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Finished Processing {@Request}", request);
    }
}