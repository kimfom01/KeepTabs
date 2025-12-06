using System.Text.Json.Serialization;
using Asp.Versioning;
using Hangfire;
using Hangfire.PostgreSql;
using KeepTabs.Domain.Common;
using KeepTabs.Infrastructure.Database;
using KeepTabs.Infrastructure.Identity;
using KeepTabs.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace KeepTabs.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddWebServices()
        {
            services.AddOpenApi();
            services.ConfigureOpenApiDocument();
            services.AddAuthorization();
            services.AddHttpClient();
            services.ConfigureIdentity();
            services.AddEndpointsApiExplorer();
            services.AddHttpContextAccessor();
            services.ConfigureHangfire();
            services.ConfigureApiVersioning();
            services.ConfigureForwardedHeadersOptions();
            services.AddHttpClient();
            services.ConfigureJsonSerialization();

            services.AddScoped<IUser, CurrentUser>();
        }

        private void ConfigureOpenApiDocument()
        {
            services.AddOpenApiDocument((configure, _) =>
            {
                configure.Title = "KeepTabs API";
                configure.Description = "OpenAPI Docs for KeepTabs API.";
                configure.Version = "v1";
            });
        }

        private void ConfigureApiVersioning()
        {
            services.AddApiVersioning(options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1, 0);
                    options.ReportApiVersions = true;
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ApiVersionReader = new UrlSegmentApiVersionReader();
                })
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'V";
                    options.SubstituteApiVersionInUrl = true;
                });
        }

        private void ConfigureHangfire()
        {
            services.AddHangfireServer(options => { options.ServerName = "KeepTabs Hangfire Server"; });
            services.AddHangfire((provider, hangfireConfig) =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                hangfireConfig.UsePostgreSqlStorage(options =>
                    options.UseNpgsqlConnection(configuration.GetConnectionString("keeptabsdb")));
            });
        }

        private void ConfigureForwardedHeadersOptions()
        {
            services.Configure<ForwardedHeadersOptions>(opt =>
            {
                opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
        }

        private void ConfigureIdentity()
        {
            services.AddIdentityApiEndpoints<ApplicationUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.User.RequireUniqueEmail = true;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
        }

        private void ConfigureJsonSerialization()
        {
            services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
        }
    }
}