using KeepTabs.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeepTabs.Infrastructure.Database.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.FirstName).HasMaxLength(ApplicationUser.MaxNameLength);
        builder.Property(user => user.LastName).HasMaxLength(ApplicationUser.MaxNameLength);
        builder.Property(user => user.ApiKeyHash)
            .HasColumnName("ApiKey")
            .HasMaxLength(ApplicationUser.MaxApiKeyHashLength);
        builder.HasIndex(user => user.ApiKeyHash)
            .IsUnique()
            .HasDatabaseName("IX_Users_ApiKeyHash")
            .HasFilter("\"ApiKey\" IS NOT NULL");
    }
}
