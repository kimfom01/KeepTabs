using KeepTabs.Domain;
using Microsoft.EntityFrameworkCore;
using DomainMonitor = KeepTabs.Domain.Monitor;

namespace KeepTabs.Application.Common.Interfaces;

/// <summary>
/// Persistence abstraction for KeepTabs aggregates.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<DomainMonitor> Monitors { get; }
    DbSet<MonitorCheck> MonitorChecks { get; }
    DbSet<AlertRule> AlertRules { get; }
    DbSet<AlertLog> AlertLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
