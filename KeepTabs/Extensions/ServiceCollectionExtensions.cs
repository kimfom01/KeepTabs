using System.Reflection;
using Asp.Versioning;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;

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

            hangfireConfig.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
            hangfireConfig.UseSimpleAssemblyNameTypeSerializer();
            hangfireConfig.UseRecommendedSerializerSettings();
            hangfireConfig.UseMongoStorage(configuration.GetConnectionString("MongoConnection"),
                new MongoStorageOptions
                {
                    MigrationOptions = new MongoMigrationOptions
                    {
                        MigrationStrategy = new MigrateMongoMigrationStrategy(),
                        BackupStrategy = new CollectionMongoBackupStrategy()
                    },
                    CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection,
                    CheckConnection = true,
                });
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
}