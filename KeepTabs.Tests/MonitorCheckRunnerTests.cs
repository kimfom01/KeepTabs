using KeepTabs.Application;
using KeepTabs.Application.Monitoring;
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
public sealed class MonitorCheckRunnerTests(PostgresFixture database)
{
    [Fact]
    public async Task MonitorCheckPersistsNewCheckRowAndMonitorState()
    {
        await using var provider = await CreateCheckServicesAsync();

        Guid monitorId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureCreatedAsync();
            var monitor = new DomainMonitor
            {
                UserId = "user-1",
                Name = "Site",
                Url = "https://example.com",
                Protocol = ProtocolType.Tcp,
            };
            db.Monitors.Add(monitor);
            await db.SaveChangesAsync();
            monitorId = monitor.Id;
        }

        bool completed;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var runner = new MonitorCheckRunner(
                db,
                [new StubProbe(new MonitorProbeResult(true, 200, 12, null))],
                TimeProvider.System,
                NullLogger<MonitorCheckRunner>.Instance);
            completed = await runner.RunCheckAsync(monitorId);
        }

        Assert.True(completed);
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await db.MonitorChecks.CountAsync());
            var monitor = await db.Monitors.SingleAsync(m => m.Id == monitorId);
            Assert.True(monitor.LastStatusUp);
            Assert.NotNull(monitor.LastCheckedAt);
        }
    }

    [Fact]
    public async Task MonitorCheckDroppedWhenMonitorDeletedMidCheck()
    {
        await using var provider = await CreateCheckServicesAsync();

        Guid monitorId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureCreatedAsync();
            var monitor = new DomainMonitor
            {
                UserId = "user-1",
                Name = "Site",
                Url = "https://example.com",
                Protocol = ProtocolType.Tcp,
            };
            db.Monitors.Add(monitor);
            await db.SaveChangesAsync();
            monitorId = monitor.Id;
        }

        bool completed;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var runner = new MonitorCheckRunner(
                db,
                [new DeletingProbe(provider.GetRequiredService<IServiceScopeFactory>(), monitorId)],
                TimeProvider.System,
                NullLogger<MonitorCheckRunner>.Instance);
            completed = await runner.RunCheckAsync(monitorId);
        }

        Assert.False(completed);
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(0, await db.MonitorChecks.CountAsync());
        }
    }

    private async Task<ServiceProvider> CreateCheckServicesAsync()
    {
        var connectionString = await database.CreateDatabaseAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMonitorChecking();
        services.AddMonitorCheckingInfrastructure();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        var provider = services.BuildServiceProvider();

        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

        return provider;
    }

    private sealed class StubProbe(MonitorProbeResult result) : IMonitorProbe
    {
        public bool CanHandle(ProtocolType protocol) => true;

        public Task<MonitorProbeResult> CheckAsync(DomainMonitor monitor, CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }

    private sealed class DeletingProbe(IServiceScopeFactory scopes, Guid monitorId) : IMonitorProbe
    {
        public bool CanHandle(ProtocolType protocol) => true;

        public async Task<MonitorProbeResult> CheckAsync(DomainMonitor monitor, CancellationToken cancellationToken)
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var current = await db.Monitors.SingleAsync(m => m.Id == monitorId, cancellationToken);
            db.Monitors.Remove(current);
            await db.SaveChangesAsync(cancellationToken);

            return new MonitorProbeResult(false, null, 1, "deleted mid-check");
        }
    }
}
