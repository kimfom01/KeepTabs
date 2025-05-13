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
}