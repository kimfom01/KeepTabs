using System.Text.Json.Serialization;
using KeepTabs.Domain.Common;
using KeepTabs.Middleware;
using KeepTabs.Services;
using Microsoft.AspNetCore.HttpOverrides;

namespace KeepTabs.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddWebServices(IConfiguration configuration)
        {
            services.AddOpenApi();
            services.AddEndpointsApiExplorer();
            services.AddProblemDetails();
            services.AddExceptionHandler<ApiExceptionHandler>();
            services.AddValidation();
            services.AddHttpClient();
            services.AddKeepTabsAuthentication();
            services.AddHttpContextAccessor();
            services.ConfigureCors(configuration);
            services.ConfigureForwardedHeadersOptions();
            services.ConfigureJsonSerialization();
            services.AddScoped<IUser, CurrentUser>();
        }

        private void ConfigureForwardedHeadersOptions()
        {
            services.Configure<ForwardedHeadersOptions>(opt =>
            {
                opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
        }

        private void ConfigureJsonSerialization()
        {
            services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        }

        private void ConfigureCors(IConfiguration configuration)
        {
            // AllowedOrigins is configured as a comma-separated scalar (appsettings
            // or Cors__AllowedOrigins env var), so parse it explicitly instead of
            // binding. Array syntax is still honored when present.
            var section = configuration.GetSection($"{CorsOptions.SectionName}:AllowedOrigins");
            var allowedOrigins = section.GetChildren().Any()
                ? CorsOptions.NormalizeOrigins(section.Get<string[]>())
                : CorsOptions.ParseAllowedOrigins(section.Value);

            services.AddOptions<CorsOptions>()
                .Configure(options => options.AllowedOrigins = allowedOrigins)
                .ValidateDataAnnotations()
                .Validate(options => options.AllowedOrigins.Length > 0, "Cors:AllowedOrigins must contain at least one origin.")
                .ValidateOnStart();

            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicies.Frontend, policy => policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            });
        }
    }
}
