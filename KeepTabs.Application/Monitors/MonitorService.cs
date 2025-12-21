using KeepTabs.Application.Monitors.Dtos;
using KeepTabs.Domain;
using KeepTabs.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace KeepTabs.Application.Monitors;

public sealed class MonitorService : IMonitorService
{
    private readonly ApplicationDbContext _dbContext;

    public MonitorService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetMonitorResponse> CreateMonitor(CreateMonitorRequest request,
        CancellationToken cancellationToken = default)
    {
        var monitor = request.ToEntity();

        var entry = await _dbContext.Monitors.AddAsync(monitor, cancellationToken);
        
        monitor.AddDomainEvent(new MonitorCreateEvent
        {
            MonitorId = monitor.Id,
            UserId = request.UserId
        });
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        return entry.Entity.ToResponse();
    }

    public async Task<GetMonitorResponse?> GetMonitorById(Guid monitorId, CancellationToken cancellationToken)
    {
        var monitor = await _dbContext.Monitors
            .Where(m => m.Id == monitorId)
            .FirstOrDefaultAsync(cancellationToken);

        return monitor?.ToResponse();
    }

    public IEnumerable<GetMonitorResponse> GetMonitors(string userId)
    {
        var monitors = _dbContext.Monitors
            .Where(m => m.UserId == userId)
            .AsNoTracking();

        return monitors.Select(m => m.ToResponse());
    }
}