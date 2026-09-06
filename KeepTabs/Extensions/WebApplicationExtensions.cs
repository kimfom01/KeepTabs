using Hangfire;
using Hangfire.Dashboard.BasicAuthorization;
using KeepTabs.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace KeepTabs.Extensions;

public static class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        public void SetupHangfireDashboard()
        {
            app.UseHangfireDashboard(options: new DashboardOptions
            {
                DashboardTitle = "KeepTabs Hangfire Dashboard",
                DisplayStorageConnectionString = app.Environment.IsDevelopment(),
                Authorization = app.Environment.IsDevelopment()
                    ? []
                    :
                    [
                        new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
                        {
                            RequireSsl = false,
                            SslRedirect = false,
                            LoginCaseSensitive = false,
                            Users =
                            [
                                new BasicAuthAuthorizationUser
                                {
                                    Login = "keeptabs",
                                    PasswordClear = "keeptabs"
                                }
                            ]
                        })
                    ],
            });
        }

        public void SetupSwaggerDocs()
        {
            if (!app.Environment.IsDevelopment())
            {
                return;
            }

            app.MapOpenApi();
            app.UseSwaggerUi(options => { options.DocumentPath = "/openapi/v1.json"; });
        }

        public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Migrations");

            logger.LogInformation("Applying database migrations.");
            await context.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations completed.");
        }
    }
}
