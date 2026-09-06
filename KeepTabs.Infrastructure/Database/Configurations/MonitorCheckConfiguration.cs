using KeepTabs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeepTabs.Infrastructure.Database.Configurations;

public class MonitorCheckConfiguration : IEntityTypeConfiguration<MonitorCheck>
{
    public void Configure(EntityTypeBuilder<MonitorCheck> builder)
    {
        builder.HasIndex(check => new { check.MonitorId, check.Timestamp });
    }
}