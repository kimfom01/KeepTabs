using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace KeepTabs.Infrastructure.Database;

/// <summary>
/// Design-time factory used by EF Core tooling.
/// </summary>
public sealed class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var contentRoot = FindApiContentRoot();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(contentRoot)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("keeptabsdb")
            ?? throw new InvalidOperationException("Connection string 'keeptabsdb' was not found for design-time migrations.");
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static string FindApiContentRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "KeepTabs", "appsettings.json");
            if (File.Exists(candidate))
            {
                return Path.Combine(directory.FullName, "KeepTabs");
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the KeepTabs API content root for design-time migrations.");
    }
}
