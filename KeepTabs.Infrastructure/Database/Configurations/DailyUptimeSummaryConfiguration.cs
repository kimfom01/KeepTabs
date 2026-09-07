using KeepTabs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeepTabs.Infrastructure.Database.Configurations;

public class DailyUptimeSummaryConfiguration : IEntityTypeConfiguration<DailyUptimeSummary>
{
    public void Configure(EntityTypeBuilder<DailyUptimeSummary> builder)
    {
        builder.HasKey(summary => new { summary.MonitorId, summary.Date });
    }
}
