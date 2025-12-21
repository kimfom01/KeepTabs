using KeepTabs.Application.Monitors.Dtos;
using Monitor = KeepTabs.Domain.Monitor;

namespace KeepTabs.Application.Monitors;

public static class MonitorMappings
{
    public static Monitor ToEntity(this CreateMonitorRequest request)
    {
        return new Monitor
        {
            Id = Guid.CreateVersion7(),
            UserId = request.UserId,
            Name = request.Name,
            Url = request.Url,
            Protocol = request.Protocol,
            CheckIntervalSeconds = request.CheckIntervalSeconds,
            TimeoutSeconds = request.TimeoutSeconds,
            ExpectedStatusCode = request.ExpectedStatusCode
        };
    }

    public static GetMonitorResponse ToResponse(this Monitor monitor)
    {
        return new GetMonitorResponse(
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
            monitor.LastStatusUp
        );
    }
}