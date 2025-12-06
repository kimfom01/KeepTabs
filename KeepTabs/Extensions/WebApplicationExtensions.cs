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
            app.MapOpenApi();
            app.UseSwaggerUi(options => { options.DocumentPath = "/openapi/v1.json"; });
        }

        public async Task ApplyMigrations()
        {
            var context = app.Services.CreateAsyncScope()
                .ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.MigrateAsync();
        }
    }
}