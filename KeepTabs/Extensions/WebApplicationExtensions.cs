using Hangfire;
using Hangfire.Dashboard.BasicAuthorization;
using KeepTabs.Database;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace KeepTabs.Extensions;

public static class WebApplicationExtensions
{
    public static void SetupHangfireDashboard(this WebApplication app)
    {
        app.UseHangfireDashboard(options: new DashboardOptions
        {
            DashboardTitle = "KeepTabs Hangfire Dashboard",
            DisplayStorageConnectionString = false,
            Authorization =
            [
                new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
                {
                    RequireSsl = !app.Environment.IsDevelopment(),
                    SslRedirect = !app.Environment.IsDevelopment(),
                    Users = [new BasicAuthAuthorizationUser
                    {
                        Login = "keeptabs",
                        PasswordClear = "keeptabs"
                    }]
                })
            ],
        });
    }

    public static void SetupScalarDocs(this WebApplication app)
    {
        app.UseSwagger(options => { options.RouteTemplate = "openapi/{documentName}.json"; });
        app.MapScalarApiReference(options => { options.WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl); });
    }

    public static void ApplyMigrations(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            var context = app.Services.CreateAsyncScope()
                .ServiceProvider.GetRequiredService<KeepTabsDbContext>();

            context.Database.Migrate();
        }
    }
}