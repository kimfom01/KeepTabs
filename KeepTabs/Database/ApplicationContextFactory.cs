using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KeepTabs.Database;

public class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private readonly IConfiguration? _configuration;

    public ApplicationContextFactory()
    {
    }

    public ApplicationContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(_configuration?.GetConnectionString("keeptabsdb"));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}