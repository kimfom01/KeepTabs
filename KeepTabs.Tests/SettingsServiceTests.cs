using System.Net;
using KeepTabs.Application.Alerts;
using KeepTabs.Application.Settings;
using KeepTabs.Application.Settings.Dtos;
using KeepTabs.Domain;
using KeepTabs.Infrastructure;
using KeepTabs.Infrastructure.Alerts;
using KeepTabs.Infrastructure.Database;
using KeepTabs.Tests.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using DomainMonitor = KeepTabs.Domain.Monitor;

namespace KeepTabs.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class SettingsServiceTests(PostgresFixture database)
{
    private static async Task<ServiceProvider> CreateServicesAsync(PostgresFixture database)
    {
        var connectionString = await database.CreateDatabaseAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMonitorCheckingInfrastructure();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        var provider = services.BuildServiceProvider();

        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

        return provider;
    }

    private static async Task UseServiceAsync(ServiceProvider provider, Func<ISettingsService, Task> action)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await action(new SettingsService(db, TimeProvider.System));
    }

    private static UpdateSmtpSettingsRequest ValidRequest(
        bool enabled = true,
        string host = "smtp.example.com",
        int port = 587,
        string password = "secret") =>
        new(enabled, host, port, "ops", password, "alerts@example.com", true);

    [Fact]
    public async Task DefaultsWhenNothingStored()
    {
        await using var provider = await CreateServicesAsync(database);

        await UseServiceAsync(provider, async service =>
        {
            var smtp = await service.GetSmtpAsync();

            Assert.False(smtp.Enabled);
            Assert.Equal(587, smtp.Port);
            Assert.True(smtp.EnableSsl);

            var view = await service.GetSmtpViewAsync();

            Assert.False(view.PasswordSet);
        });
    }

    [Fact]
    public async Task SaveAndReadBackMasksPassword()
    {
        await using var provider = await CreateServicesAsync(database);

        await UseServiceAsync(provider, async service =>
        {
            var view = await service.UpdateSmtpAsync(ValidRequest());

            Assert.True(view.Enabled);
            Assert.Equal("smtp.example.com", view.Host);
            Assert.True(view.PasswordSet);

            var stored = await service.GetSmtpAsync();

            Assert.Equal("secret", stored.Password);
        });
    }

    [Fact]
    public async Task EmptyPasswordKeepsStoredOne()
    {
        await using var provider = await CreateServicesAsync(database);

        await UseServiceAsync(provider, async service =>
        {
            await service.UpdateSmtpAsync(ValidRequest(password: "first"));
            var view = await service.UpdateSmtpAsync(ValidRequest(password: ""));

            Assert.True(view.PasswordSet);
            Assert.Equal("first", (await service.GetSmtpAsync()).Password);

            var replaced = await service.UpdateSmtpAsync(ValidRequest(password: "second"));

            Assert.True(replaced.PasswordSet);
            Assert.Equal("second", (await service.GetSmtpAsync()).Password);
        });
    }

    [Fact]
    public async Task CorruptDocumentFallsBackToDefaults()
    {
        await using var provider = await CreateServicesAsync(database);

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.AppSettings.Add(new AppSetting { Key = "smtp", Value = "{not json", UpdatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        await UseServiceAsync(provider, async service =>
        {
            Assert.False((await service.GetSmtpAsync()).Enabled);
        });
    }

    [Fact]
    public async Task EmailChannelReportsMisconfiguration()
    {
        var disabled = new EmailAlertChannel(new FixedSettings(new SmtpSettings(
            false, string.Empty, 587, string.Empty, string.Empty, string.Empty, true)));
        var unconfigured = new EmailAlertChannel(new FixedSettings(new SmtpSettings(
            true, string.Empty, 587, string.Empty, string.Empty, string.Empty, true)));
        var monitor = new DomainMonitor { UserId = "user-1", Name = "Site", Url = "https://example.com" };
        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            MonitorId = monitor.Id,
            Type = AlertType.Email,
            TriggerType = AlertTriggerType.OnDown,
            Target = "ops@example.com",
        };

        var missing = await disabled.SendAsync(rule, monitor, false, "down");
        var noHost = await unconfigured.SendAsync(rule, monitor, false, "down");

        Assert.False(missing.Success);
        Assert.Contains("Settings", missing.Error);
        Assert.False(noHost.Success);
    }

    [Theory]
    [InlineData(true, "", 587, "alerts@example.com", false)]
    [InlineData(true, "smtp.example.com", 0, "alerts@example.com", false)]
    [InlineData(true, "smtp.example.com", 587, "not-an-email", false)]
    [InlineData(true, "smtp.example.com", 587, "alerts@example.com", true)]
    [InlineData(false, "", 587, "", true)]
    public void SmtpRequestValidation(
        bool enabled, string host, int port, string from, bool expectedValid)
    {
        var validator = new UpdateSmtpSettingsRequestValidator();
        var request = new UpdateSmtpSettingsRequest(enabled, host, port, "ops", "", from, true);

        Assert.Equal(expectedValid, validator.Validate(request).IsValid);
    }

    [Fact]
    public async Task TelegramTokenRoundtripsMasked()
    {
        await using var provider = await CreateServicesAsync(database);

        await UseServiceAsync(provider, async service =>
        {
            var empty = await service.GetTelegramViewAsync();

            Assert.False(empty.Enabled);
            Assert.False(empty.TokenSet);

            var saved = await service.UpdateTelegramAsync(new UpdateTelegramSettingsRequest(true, "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11"));

            Assert.True(saved.Enabled);
            Assert.True(saved.TokenSet);
            Assert.Equal("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11", (await service.GetTelegramAsync()).BotToken);

            var kept = await service.UpdateTelegramAsync(new UpdateTelegramSettingsRequest(false, ""));

            Assert.False(kept.Enabled);
            Assert.True(kept.TokenSet);
        });
    }

    [Fact]
    public async Task TelegramChannelPostsToBotApi()
    {
        var factory = new CaptureFactory("""{"ok":true}""");
        var settings = new FixedSettings(
            telegram: new TelegramSettings(true, "TOKEN"),
            smtp: new SmtpSettings(false, string.Empty, 587, string.Empty, string.Empty, string.Empty, true));
        var channel = new TelegramAlertChannel(factory, settings, NullLogger<TelegramAlertChannel>.Instance);
        var monitor = new DomainMonitor { UserId = "user-1", Name = "Site", Url = "https://example.com" };
        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            MonitorId = monitor.Id,
            Type = AlertType.Telegram,
            TriggerType = AlertTriggerType.OnDown,
            Target = "-100123",
        };

        var result = await channel.SendAsync(rule, monitor, false, "down");

        Assert.True(result.Success);
        var request = Assert.Single(factory.Requests);
        Assert.Equal("https://api.telegram.org/botTOKEN/sendMessage", request.RequestUri?.ToString());
        Assert.Contains("-100123", await request.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task TelegramChannelReportsApiAndConfigFailures()
    {
        var monitor = new DomainMonitor { UserId = "user-1", Name = "Site", Url = "https://example.com" };
        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            MonitorId = monitor.Id,
            Type = AlertType.Telegram,
            TriggerType = AlertTriggerType.OnDown,
            Target = "-100123",
        };

        var unconfigured = new TelegramAlertChannel(
            new CaptureFactory("""{"ok":true}"""),
            new FixedSettings(
                telegram: new TelegramSettings(false, string.Empty),
                smtp: new SmtpSettings(false, string.Empty, 587, string.Empty, string.Empty, string.Empty, true)),
            NullLogger<TelegramAlertChannel>.Instance);
        var rejected = new TelegramAlertChannel(
            new CaptureFactory("""{"ok":false,"description":"chat not found"}""", HttpStatusCode.BadRequest),
            new FixedSettings(
                telegram: new TelegramSettings(true, "TOKEN"),
                smtp: new SmtpSettings(false, string.Empty, 587, string.Empty, string.Empty, string.Empty, true)),
            NullLogger<TelegramAlertChannel>.Instance);

        var missing = await unconfigured.SendAsync(rule, monitor, false, "down");
        var failed = await rejected.SendAsync(rule, monitor, false, "down");

        Assert.False(missing.Success);
        Assert.Contains("Settings", missing.Error);
        Assert.False(failed.Success);
    }

    private sealed class FixedSettings(SmtpSettings smtp, TelegramSettings? telegram = null) : ISettingsService
    {
        public Task<SmtpSettings> GetSmtpAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(smtp);

        public Task<SmtpSettingsResponse> GetSmtpViewAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SmtpSettingsResponse> UpdateSmtpAsync(
            UpdateSmtpSettingsRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TelegramSettings> GetTelegramAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(telegram ?? new TelegramSettings(false, string.Empty));

        public Task<TelegramSettingsResponse> GetTelegramViewAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TelegramSettingsResponse> UpdateTelegramAsync(
            UpdateTelegramSettingsRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class CaptureFactory(string payload, HttpStatusCode status = HttpStatusCode.OK) : IHttpClientFactory
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public HttpClient CreateClient(string name) => new(new CaptureHandler(this, payload, status));

        private sealed class CaptureHandler(CaptureFactory parent, string payload, HttpStatusCode status) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                parent.Requests.Add(request);

                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(payload),
                });
            }
        }
    }
}
