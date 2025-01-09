using Hangfire;
using Scalar.AspNetCore;

namespace KeepTabs.Extensions;

public static class WebApplicationExtensions
{
    public static void SetupHangfireDashboard(this WebApplication app)
    {
        app.UseHangfireDashboard(options: new DashboardOptions
        {
            DashboardTitle = "KeepTabs Hangfire Dashboard",
            DisplayStorageConnectionString = false
        });
    }

    public static void SetupScalarDocs(this WebApplication app)
    {
        app.UseSwagger(options => { options.RouteTemplate = "openapi/{documentName}.json"; });
        app.MapScalarApiReference(options => { options.WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl); });
    }
}