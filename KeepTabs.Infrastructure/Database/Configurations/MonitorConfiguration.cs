using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monitor = KeepTabs.Domain.Monitor;

namespace KeepTabs.Infrastructure.Database.Configurations;

public class MonitorConfiguration : IEntityTypeConfiguration<Monitor>
{
    public void Configure(EntityTypeBuilder<Monitor> builder)
    {
        builder.HasIndex(monitor => new { monitor.UserId, monitor.Name }).IsUnique();
        builder.Property(monitor => monitor.Url).HasMaxLength(1024);
    }
}
