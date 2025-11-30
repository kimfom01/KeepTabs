using KeepTabs.Entities;
using KeepTabs.Entities.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Monitor = KeepTabs.Entities.Monitor;

namespace KeepTabs.Database;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public DbSet<AlertLog> AlertLogs { get; set; }
    public DbSet<AlertRule> AlertRules { get; set; }
    public DbSet<Monitor> Monitors { get; set; }
    public DbSet<MonitorCheck> MonitorChecks { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity => entity.ToTable(name: "Users"));
        builder.Entity<IdentityRole>(entity => entity.ToTable(name: "Roles"));
        builder.Entity<IdentityUserRole<string>>(entity => entity.ToTable("UserRoles"));
        builder.Entity<IdentityUserClaim<string>>(entity => entity.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<string>>(entity => entity.ToTable("UserLogins"));
        builder.Entity<IdentityRoleClaim<string>>(entity => entity.ToTable("RoleClaims"));
        builder.Entity<IdentityUserToken<string>>(entity => entity.ToTable("UserTokens"));
        
        builder.Entity<Monitor>(typeBuilder =>
        {
            typeBuilder.HasIndex(monitor => new { monitor.UserId, monitor.Name }).IsUnique();
            typeBuilder.Property(monitor => monitor.Url).HasMaxLength(1024);
        });

        builder.Entity<MonitorCheck>(typeBuilder =>
        {
            typeBuilder.HasIndex(check => new { check.MonitorId, check.Timestamp });
        });

        builder.Entity<AlertRule>(typeBuilder =>
        {
            typeBuilder.HasIndex(rule => new { rule.MonitorId, rule.IsEnabled });
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(entry => entry.State == EntityState.Added);

        foreach (var entry in entries)
        {
            entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}