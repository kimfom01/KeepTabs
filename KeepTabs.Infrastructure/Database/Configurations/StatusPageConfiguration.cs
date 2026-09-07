using KeepTabs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeepTabs.Infrastructure.Database.Configurations;

public class StatusPageConfiguration : IEntityTypeConfiguration<StatusPage>
{
    public void Configure(EntityTypeBuilder<StatusPage> builder)
    {
        builder.HasIndex(page => page.Slug).IsUnique();
        builder.Property(page => page.Name).HasMaxLength(StatusPage.MaxNameLength);
        builder.Property(page => page.Slug).HasMaxLength(StatusPage.MaxSlugLength);
        builder
            .HasMany(page => page.Monitors)
            .WithOne(link => link.StatusPage)
            .HasForeignKey(link => link.StatusPageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StatusPageMonitorConfiguration : IEntityTypeConfiguration<StatusPageMonitor>
{
    public void Configure(EntityTypeBuilder<StatusPageMonitor> builder)
    {
        builder.HasKey(link => new { link.StatusPageId, link.MonitorId });
    }
}
