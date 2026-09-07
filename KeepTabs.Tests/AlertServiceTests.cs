using FluentValidation;
using KeepTabs.Application.Alerts;
using KeepTabs.Application.Alerts.Dtos;
using KeepTabs.Domain;
using KeepTabs.Infrastructure;
using KeepTabs.Infrastructure.Database;
using KeepTabs.Tests.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainMonitor = KeepTabs.Domain.Monitor;

namespace KeepTabs.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class AlertServiceTests(PostgresFixture database)
{
    private static readonly CreateAlertRuleRequest EmailRule = new(
        Guid.Empty, AlertType.Email, AlertTriggerType.OnDown, 3, 30, "ops@example.com", true);

    private static async Task<(ServiceProvider Provider, FakeDispatcher Dispatcher)> CreateServicesAsync(
        PostgresFixture database)
    {
        var connectionString = await database.CreateDatabaseAsync();
        var dispatcher = new FakeDispatcher();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMonitorCheckingInfrastructure();
        services.AddSingleton<IAlertDispatcher>(dispatcher);
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        var provider = services.BuildServiceProvider();

        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

        return (provider, dispatcher);
    }

    private static AlertService CreateService(ApplicationDbContext db, FakeDispatcher dispatcher) =>
        new(db, new CreateAlertRuleRequestValidator(), dispatcher, TimeProvider.System);

    private static async Task<DomainMonitor> SeedMonitorAsync(ApplicationDbContext db, string userId = "user-1")
    {
        var monitor = new DomainMonitor
        {
            UserId = userId,
            Name = $"Site-{Guid.NewGuid():N}",
            Url = "https://example.com",
            Protocol = ProtocolType.Http,
        };
        db.Monitors.Add(monitor);
        await db.SaveChangesAsync();

        return monitor;
    }

    private static async Task UseScopeAsync(ServiceProvider provider, Func<ApplicationDbContext, Task> action)
    {
        await using var scope = provider.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    [Fact]
    public async Task CreateAssignsRuleToOwnedMonitor()
    {
        var (provider, dispatcher) = await CreateServicesAsync(database);
        await using var _provider = provider;

        Guid monitorId = Guid.Empty;
        await UseScopeAsync(provider, async db => { monitorId = (await SeedMonitorAsync(db)).Id; });

        GetAlertRuleResponse created = null!;
        await UseScopeAsync(provider, async db =>
        {
            var service = CreateService(db, dispatcher);
            created = await service.CreateAsync("user-1", EmailRule with { MonitorId = monitorId });
        });

        Assert.Equal(monitorId, created.MonitorId);
        Assert.Equal(AlertType.Email, created.Type);
        Assert.True(created.IsEnabled);
        Assert.Null(created.LastFiredAt);
    }

    [Fact]
    public async Task CreateRejectsForeignMonitor()
    {
        var (provider, dispatcher) = await CreateServicesAsync(database);
        await using var _provider = provider;

        Guid monitorId = Guid.Empty;
        await UseScopeAsync(provider, async db => { monitorId = (await SeedMonitorAsync(db, "user-2")).Id; });

        await UseScopeAsync(provider, async db =>
        {
            var service = CreateService(db, dispatcher);
            await Assert.ThrowsAsync<ValidationException>(() =>
                service.CreateAsync("user-1", EmailRule with { MonitorId = monitorId }));
        });
    }

    [Fact]
    public async Task ListScopedToOwnerAndFilterableByMonitor()
    {
        var (provider, dispatcher) = await CreateServicesAsync(database);
        await using var _provider = provider;

        Guid first = Guid.Empty, second = Guid.Empty;
        await UseScopeAsync(provider, async db =>
        {
            first = (await SeedMonitorAsync(db, "user-1")).Id;
            second = (await SeedMonitorAsync(db, "user-1")).Id;
            await SeedMonitorAsync(db, "user-2");
            var service = CreateService(db, dispatcher);
            await service.CreateAsync("user-1", EmailRule with { MonitorId = first });
            await service.CreateAsync("user-1", EmailRule with
            {
                MonitorId = second,
                Type = AlertType.Webhook,
                Target = "https://hooks.example.com/x",
            });
            var otherMonitor = await db.Monitors.FirstAsync(m => m.UserId == "user-2");
            await service.CreateAsync("user-2", EmailRule with { MonitorId = otherMonitor.Id });
        });

        await UseScopeAsync(provider, async db =>
        {
            var service = CreateService(db, dispatcher);
            Assert.Equal(2, (await service.ListAsync("user-1", null)).Count);
            Assert.Single(await service.ListAsync("user-1", first));
            Assert.Empty(await service.ListAsync("user-1", Guid.NewGuid()));
        });
    }

    [Fact]
    public async Task GetHonorsOwnership()
    {
        var (provider, dispatcher) = await CreateServicesAsync(database);
        await using var _provider = provider;

        Guid ruleId = Guid.Empty;
        await UseScopeAsync(provider, async db =>
        {
            var monitor = await SeedMonitorAsync(db);
            var service = CreateService(db, dispatcher);
            ruleId = (await service.CreateAsync("user-1", EmailRule with { MonitorId = monitor.Id })).AlertRuleId;
        });

        await UseScopeAsync(provider, async db =>
        {
            var service = CreateService(db, dispatcher);
            Assert.NotNull(await service.GetByIdAsync("user-1", ruleId));
            Assert.Null(await service.GetByIdAsync("user-2", ruleId));
            Assert.Null(await service.GetByIdAsync("user-1", Guid.NewGuid()));
        });
    }

    [Fact]
    public async Task UpdateAppliesPartialChangesAndRejectsMismatch()
    {
        var (provider, dispatcher) = await CreateServicesAsync(database);
        await using var _provider = provider;

        Guid ruleId = Guid.Empty;
        await UseScopeAsync(provider, async db =>
        {
            var monitor = await SeedMonitorAsync(db);
            var service = CreateService(db, dispatcher);
            ruleId = (await service.CreateAsync("user-1", EmailRule with { MonitorId = monitor.Id })).AlertRuleId;
        });

        await UseScopeAsync(provider, async db =>
        {
            var service = CreateService(db, dispatcher);
            var updated = await service.UpdateAsync(
                "user-1", ruleId, new UpdateAlertRuleRequest(null, null, 5, null, null, false));

            Assert.NotNull(updated);
            Assert.Equal(5, updated.Threshold);
            Assert.False(updated.IsEnabled);
            Assert.Equal(AlertType.Email, updated.Type);

            // Switching an email-targeted rule to webhook must fail effective validation.
            await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(
                "user-1", ruleId, new UpdateAlertRuleRequest(AlertType.Webhook, null, null, null, null, null)));

            await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(
                "user-1", ruleId, new UpdateAlertRuleRequest(null, null, null, null, null, null)));

            Assert.Null(await service.UpdateAsync(
                "user-2", ruleId, new UpdateAlertRuleRequest(null, null, 5, null, null, null)));
        });
    }

    [Fact]
    public async Task DeleteRemovesOwnedRuleOnly()
    {
        var (provider, dispatcher) = await CreateServicesAsync(database);
        await using var _provider = provider;

        Guid ruleId = Guid.Empty;
        await UseScopeAsync(provider, async db =>
        {
            var monitor = await SeedMonitorAsync(db);
            var service = CreateService(db, dispatcher);
            ruleId = (await service.CreateAsync("user-1", EmailRule with { MonitorId = monitor.Id })).AlertRuleId;
        });

        await UseScopeAsync(provider, async db =>
        {
            var service = CreateService(db, dispatcher);
            Assert.False(await service.DeleteAsync("user-2", ruleId));
            Assert.True(await service.DeleteAsync("user-1", ruleId));
            Assert.Null(await service.GetByIdAsync("user-1", ruleId));
        });
    }

    [Fact]
    public async Task LogsScopedAndOrdered()
    {
        var (provider, dispatcher) = await CreateServicesAsync(database);
        await using var _provider = provider;

        Guid first = Guid.Empty;
        await UseScopeAsync(provider, async db =>
        {
            var monitor = await SeedMonitorAsync(db);
            first = monitor.Id;
            var other = await SeedMonitorAsync(db);
            var service = CreateService(db, dispatcher);
            var rule = await service.CreateAsync("user-1", EmailRule with { MonitorId = first });
            await service.TestAsync("user-1", rule.AlertRuleId);
            await service.TestAsync("user-1", rule.AlertRuleId);
            var otherRule = await service.CreateAsync("user-1", EmailRule with { MonitorId = other.Id });
            await service.TestAsync("user-1", otherRule.AlertRuleId);
        });

        await UseScopeAsync(provider, async db =>
        {
            var service = CreateService(db, dispatcher);
            var logs = await service.GetLogsAsync("user-1", null);

            Assert.Equal(3, logs.Count);
            Assert.True(logs[0].FiredAt >= logs[1].FiredAt);
            Assert.True(logs[1].FiredAt >= logs[2].FiredAt);
            Assert.All(logs, log => Assert.True(log.Success));
            Assert.Equal(2, (await service.GetLogsAsync("user-1", first)).Count);
            Assert.Empty(await service.GetLogsAsync("user-2", null));
        });
        Assert.Equal(3, dispatcher.Calls.Count);
    }

    [Fact]
    public async Task TestRecordsDeliveryFailure()
    {
        var (provider, dispatcher) = await CreateServicesAsync(database);
        await using var _provider = provider;
        dispatcher.Result = new AlertDeliveryResult(false, "SMTP delivery is not configured.");

        Guid ruleId = Guid.Empty;
        await UseScopeAsync(provider, async db =>
        {
            var monitor = await SeedMonitorAsync(db);
            var service = CreateService(db, dispatcher);
            ruleId = (await service.CreateAsync("user-1", EmailRule with { MonitorId = monitor.Id })).AlertRuleId;
        });

        await UseScopeAsync(provider, async db =>
        {
            var service = CreateService(db, dispatcher);
            var response = await service.TestAsync("user-1", ruleId);

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Equal("SMTP delivery is not configured.", response.Error);
            Assert.Null(await service.TestAsync("user-2", ruleId));
        });
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
