using Serilog;

namespace KeepTabs.Extensions;

public static class BuilderExtensions
{
    public static void ConfigureSerilog(this IHostBuilder builder)
    {
        builder.UseSerilog((context, loggerConfig) => { loggerConfig.ReadFrom.Configuration(context.Configuration); });
    }
}
