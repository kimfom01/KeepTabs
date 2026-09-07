using KeepTabs.Application.Alerts;
using KeepTabs.Domain;
using KeepTabs.Infrastructure;
using KeepTabs.Infrastructure.Database;
using KeepTabs.Tests.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using DomainMonitor = KeepTabs.Domain.Monitor;

namespace KeepTabs.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class AlertEvaluatorTests(PostgresFixture database)
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

    private static async Task<DomainMonitor> SeedMonitorAsync(ApplicationDbContext db, string name = "Site")
    {
        var monitor = new DomainMonitor
        {
            UserId = "user-1",
            Name = name,
            Url = "https://example.com",
            Protocol = ProtocolType.Http,
            LastStatusUp = true,
        };
        db.Monitors.Add(monitor);
        await db.SaveChangesAsync();

        return monitor;
    }

    private static AlertRule SeedRule(
        ApplicationDbContext db,
        Guid monitorId,
        AlertTriggerType trigger,
        int threshold = 3,
        int cooldownMinutes = 0,
        bool enabled = true,
        DateTimeOffset? lastFiredAt = null)
    {
        var rule = new AlertRule
        {
            Id = Guid.CreateVersion7(),
            MonitorId = monitorId,
            Type = AlertType.Webhook,
            TriggerType = trigger,
            Threshold = threshold,
            CoolDownMinutes = cooldownMinutes,
            IsEnabled = enabled,
            Target = "https://hooks.example.com/x",
            LastFiredAt = lastFiredAt,
        };
        db.AlertRules.Add(rule);

        return rule;
    }

    private static void SeedFailure(ApplicationDbContext db, Guid monitorId, DateTimeOffset timestamp)
    {
        db.MonitorChecks.Add(new MonitorCheck
        {
            Id = Guid.CreateVersion7(),
            MonitorId = monitorId,
            Timestamp = timestamp,
            IsUp = false,
            ResponseTimeMs = 5,
        });
    }

    private static async Task<(int Dispatches, int Logs)> EvaluateAsync(
        ServiceProvider provider, FakeDispatcher dispatcher, Guid monitorId, bool? previous, bool current)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var monitor = await db.Monitors.SingleAsync(m => m.Id == monitorId);
        var evaluator = new AlertEvaluator(
            db, dispatcher, TimeProvider.System, NullLogger<AlertEvaluator>.Instance);

        await evaluator.EvaluateAsync(monitor, previous, current);
        await db.SaveChangesAsync();

        return (dispatcher.Calls.Count, await db.AlertLogs.CountAsync());
    }

    [Fact]
    public async Task OnDownFiresOnTransitionAndCooldownSuppressesRepeat()
    {
        await using var provider = await CreateServicesAsync(database);
        var dispatcher = new FakeDispatcher();

        Guid monitorId = Guid.Empty;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            monitorId = (await SeedMonitorAsync(db)).Id;
            SeedRule(db, monitorId, AlertTriggerType.OnDown, cooldownMinutes: 30);
            await db.SaveChangesAsync();
        }

        var first = await EvaluateAsync(provider, dispatcher, monitorId, previous: true, current: false);

        // Flapping back within the 30-minute cooldown must not re-fire.
        var second = await EvaluateAsync(provider, dispatcher, monitorId, previous: true, current: false);

        Assert.Equal((1, 1), first);
        Assert.Equal((1, 1), second);
        Assert.Contains("DOWN", dispatcher.Calls[0].Message);
    }

    [Fact]
    public async Task OnDownSilentWhenAlreadyDown()
    {
        await using var provider = await CreateServicesAsync(database);
        var dispatcher = new FakeDispatcher();

        Guid monitorId = Guid.Empty;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            monitorId = (await SeedMonitorAsync(db)).Id;
            SeedRule(db, monitorId, AlertTriggerType.OnDown);
            await db.SaveChangesAsync();
        }

        var result = await EvaluateAsync(provider, dispatcher, monitorId, previous: false, current: false);

        Assert.Equal((0, 0), result);
    }

    [Fact]
    public async Task OnUpFiresOnRecoveryOnly()
    {
        await using var provider = await CreateServicesAsync(database);
        var dispatcher = new FakeDispatcher();

        Guid monitorId = Guid.Empty;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            monitorId = (await SeedMonitorAsync(db)).Id;
            SeedRule(db, monitorId, AlertTriggerType.OnUp);
            await db.SaveChangesAsync();
        }

        var recovery = await EvaluateAsync(provider, dispatcher, monitorId, previous: false, current: true);
        var steady = await EvaluateAsync(provider, dispatcher, monitorId, previous: true, current: true);

        Assert.Equal((1, 1), recovery);
        Assert.Equal((1, 1), steady);
        Assert.Contains("back UP", dispatcher.Calls[0].Message);
    }

    [Fact]
    public async Task ConsecutiveFailuresFireAtThreshold()
    {
        await using var provider = await CreateServicesAsync(database);
        var dispatcher = new FakeDispatcher();
        var now = DateTimeOffset.UtcNow;

        Guid monitorId = Guid.Empty;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            monitorId = (await SeedMonitorAsync(db)).Id;
            SeedRule(db, monitorId, AlertTriggerType.ConsecutiveFailures, threshold: 3);
            SeedFailure(db, monitorId, now.AddMinutes(-20));
            await db.SaveChangesAsync();
        }

        // One prior failure + current = 2 < 3: silent.
        var early = await EvaluateAsync(provider, dispatcher, monitorId, previous: true, current: false);

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedFailure(db, monitorId, now.AddMinutes(-10));
            await db.SaveChangesAsync();
        }

        // Two prior failures + current = 3: fires.
        var fired = await EvaluateAsync(provider, dispatcher, monitorId, previous: false, current: false);

        Assert.Equal((0, 0), early);
        Assert.Equal((1, 1), fired);
        Assert.Contains("3 checks in a row", dispatcher.Calls[0].Message);
    }

    [Fact]
    public async Task DisabledRulesNeverFire()
    {
        await using var provider = await CreateServicesAsync(database);
        var dispatcher = new FakeDispatcher();

        Guid monitorId = Guid.Empty;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            monitorId = (await SeedMonitorAsync(db)).Id;
            SeedRule(db, monitorId, AlertTriggerType.OnDown, enabled: false);
            await db.SaveChangesAsync();
        }

        var result = await EvaluateAsync(provider, dispatcher, monitorId, previous: true, current: false);

        Assert.Equal((0, 0), result);
    }

    [Fact]
    public async Task DeliveryFailureRecordedInLog()
    {
        await using var provider = await CreateServicesAsync(database);
        var dispatcher = new FakeDispatcher { Result = new AlertDeliveryResult(false, "no route") };

        Guid monitorId = Guid.Empty;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            monitorId = (await SeedMonitorAsync(db)).Id;
            SeedRule(db, monitorId, AlertTriggerType.OnDown);
            await db.SaveChangesAsync();
        }

        await EvaluateAsync(provider, dispatcher, monitorId, previous: true, current: false);

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var log = await verifyDb.AlertLogs.SingleAsync();

        Assert.False(log.Success);
        Assert.Equal("no route", log.Error);
    }

    private sealed class FakeDispatcher : IAlertDispatcher
    {
        public List<(AlertRule Rule, string Message)> Calls { get; } = [];
        public AlertDeliveryResult Result { get; set; } = new(true, null);

        public Task<AlertDeliveryResult> DispatchAsync(
            AlertRule rule, DomainMonitor monitor, bool isUp, string message,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((rule, message));

            return Task.FromResult(Result);
        }
    }
}
