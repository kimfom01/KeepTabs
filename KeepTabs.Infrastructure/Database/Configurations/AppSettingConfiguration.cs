using KeepTabs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeepTabs.Infrastructure.Database.Configurations;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.HasKey(setting => setting.Key);
        builder.Property(setting => setting.Key).HasMaxLength(200);
    }
}
