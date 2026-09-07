using KeepTabs.Application.Monitoring;
using KeepTabs.Domain;
using KeepTabs.Infrastructure;
using KeepTabs.Infrastructure.Database;
using KeepTabs.Tests.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainMonitor = KeepTabs.Domain.Monitor;

namespace KeepTabs.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class UptimeAggregatorTests(PostgresFixture database)
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

    [Fact]
    public async Task AggregatesPerMonitorDayAndRerunsIdempotently()
    {
        await using var provider = await CreateServicesAsync(database);
        var day = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var start = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        Guid first = Guid.Empty, second = Guid.Empty;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            first = (await SeedMonitorAsync(db, "one")).Id;
            second = (await SeedMonitorAsync(db, "two")).Id;
            db.MonitorChecks.AddRange(
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = first, Timestamp = start.AddHours(1),
                    IsUp = true, StatusCode = 200, ResponseTimeMs = 100,
                },
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = first, Timestamp = start.AddHours(2),
                    IsUp = true, StatusCode = 200, ResponseTimeMs = 300,
                },
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = first, Timestamp = start.AddHours(3),
                    IsUp = false, StatusCode = 500, ResponseTimeMs = 500,
                },
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = second, Timestamp = start.AddHours(1),
                    IsUp = false, StatusCode = null, ResponseTimeMs = 10,
                },
                // Outside the aggregated day: must not leak in.
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = first, Timestamp = start.AddDays(1).AddHours(1),
                    IsUp = true, StatusCode = 200, ResponseTimeMs = 10,
                });
            await db.SaveChangesAsync();
        }

        int firstRun, secondRun;
        await using (var scope = provider.CreateAsyncScope())
        {
            var aggregator = new DailyUptimeAggregator(
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
            firstRun = await aggregator.AggregateAsync(day);
            secondRun = await aggregator.AggregateAsync(day);
        }

        Assert.Equal(2, firstRun);
        Assert.Equal(2, secondRun);
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(2, await db.DailyUptimeSummaries.CountAsync(s => s.Date == day));

            var one = await db.DailyUptimeSummaries.SingleAsync(s => s.MonitorId == first);
            Assert.Equal(3, one.TotalChecks);
            Assert.Equal(2, one.UpCount);
            Assert.Equal(300, one.AverageResponseTimeMs);

            var two = await db.DailyUptimeSummaries.SingleAsync(s => s.MonitorId == second);
            Assert.Equal(1, two.TotalChecks);
            Assert.Equal(0, two.UpCount);
        }
    }

    [Fact]
    public async Task EmptyDayAggregatesZeroMonitors()
    {
        await using var provider = await CreateServicesAsync(database);

        await using var scope = provider.CreateAsyncScope();
        var aggregator = new DailyUptimeAggregator(
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());

        Assert.Equal(0, await aggregator.AggregateAsync(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))));
    }

    [Fact]
    public async Task AggregatesCompleteHoursIdempotently()
    {
        await using var provider = await CreateServicesAsync(database);
        var now = DateTimeOffset.UtcNow;
        var hour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);

        Guid monitorId = Guid.Empty;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            monitorId = (await SeedMonitorAsync(db, "hourly")).Id;
            db.MonitorChecks.AddRange(
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = monitorId, Timestamp = hour.AddMinutes(-90),
                    IsUp = true, StatusCode = 200, ResponseTimeMs = 100,
                },
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = monitorId, Timestamp = hour.AddMinutes(-80),
                    IsUp = false, StatusCode = 500, ResponseTimeMs = 200,
                },
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = monitorId, Timestamp = hour.AddMinutes(-10),
                    IsUp = true, StatusCode = 200, ResponseTimeMs = 300,
                });
            await db.SaveChangesAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var aggregator = new DailyUptimeAggregator(db);

            var buckets = await aggregator.AggregateHourlyAsync(hour.AddHours(-2), hour);

            Assert.Equal(2, buckets);
            Assert.Equal(2, await aggregator.AggregateHourlyAsync(hour.AddHours(-2), hour));

            var rows = await db.HourlyUptimeSummaries
                .Where(s => s.MonitorId == monitorId)
                .OrderBy(s => s.Hour)
                .ToListAsync();

            Assert.Equal(2, rows.Count);
            Assert.Equal(hour.AddHours(-2), rows[0].Hour);
            Assert.Equal(2, rows[0].TotalChecks);
            Assert.Equal(1, rows[0].UpCount);
            Assert.Equal(150, rows[0].AverageResponseTimeMs);
            Assert.Equal(hour.AddHours(-1), rows[1].Hour);
            Assert.Equal(1, rows[1].TotalChecks);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var aggregator = new DailyUptimeAggregator(db);

            Assert.Equal(0, await aggregator.AggregateHourlyAsync(hour, hour));
        }
    }

    private static async Task<DomainMonitor> SeedMonitorAsync(ApplicationDbContext db, string name)
    {
        var monitor = new DomainMonitor
        {
            UserId = "user-1",
            Name = name,
            Url = "https://example.com",
            Protocol = ProtocolType.Http,
        };
        db.Monitors.Add(monitor);
        await db.SaveChangesAsync();

        return monitor;
    }
}
