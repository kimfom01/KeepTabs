using KeepTabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace KeepTabs.Database;

public class KeepTabsDbContext : DbContext
{
    public DbSet<JobTracking> JobTrackings { get; set; }
    public DbSet<ResponseStatus> ResponseStatuses { get; set; }

    public KeepTabsDbContext(DbContextOptions<KeepTabsDbContext> options) : base(options)
    {
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(entry => entry.State == EntityState.Added);

        foreach (var entry in entries)
        {
            entry.Entity.CreatedAtUtc = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}