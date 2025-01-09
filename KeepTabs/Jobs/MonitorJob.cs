using KeepTabs.Requests;

namespace KeepTabs.Jobs;

public class MonitorJob
{
    private readonly ILogger<MonitorJob> _logger;

    public MonitorJob(ILogger<MonitorJob> logger)
    {
        _logger = logger;
    }
    
    public void PrintRequest(MonitorRequest request)
    {
        _logger.LogInformation("Logging Requst {@Request}", request);
    }
}