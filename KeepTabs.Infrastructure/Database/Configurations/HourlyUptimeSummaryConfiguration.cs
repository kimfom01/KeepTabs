using KeepTabs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeepTabs.Infrastructure.Database.Configurations;

public class HourlyUptimeSummaryConfiguration : IEntityTypeConfiguration<HourlyUptimeSummary>
{
    public void Configure(EntityTypeBuilder<HourlyUptimeSummary> builder)
    {
        builder.HasKey(summary => new { summary.MonitorId, summary.Hour });
    }
}
