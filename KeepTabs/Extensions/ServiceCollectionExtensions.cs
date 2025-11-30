using System.Reflection;
using Asp.Versioning;
using Hangfire;
using Hangfire.PostgreSql;
using KeepTabs.Database;
using KeepTabs.Entities;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;

namespace KeepTabs.Extensions;

public static class ServiceCollectionExtensions
{
    public static void ConfigureSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "KeepTabs API",
                Description = "OpenAPI Docs for KeepTabs API."
            });

            var xmlFilename = $"{Assembly.GetEntryAssembly()?.GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
        });
    }

    public static void ConfigureHangfire(this IServiceCollection services)
    {
        services.AddHangfireServer(options => { options.ServerName = "KeepTabs Hangfire Server"; });
        services.AddHangfire((provider, hangfireConfig) =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            hangfireConfig.UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(configuration.GetConnectionString("keeptabsdb")));
        });
    }

    public static void ConfigureApiVersioning(this IServiceCollection services)
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

    public static void ConfigureForwardedHeadersOptions(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(opt =>
        {
            opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });
    }

    public static void ConfigureIdentity(this IServiceCollection services)
    {
        services.AddIdentityApiEndpoints<User>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
    }
}