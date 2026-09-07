using FluentValidation;
using KeepTabs.Application.Common.Interfaces;
using KeepTabs.Application.StatusPages;
using KeepTabs.Application.StatusPages.Dtos;
using KeepTabs.Domain;
using KeepTabs.Infrastructure;
using KeepTabs.Infrastructure.Database;
using KeepTabs.Tests.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainMonitor = KeepTabs.Domain.Monitor;

namespace KeepTabs.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class StatusPageServiceTests(PostgresFixture database)
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

    private static StatusPageService CreateService(ApplicationDbContext db) =>
        new(db, new CreateStatusPageRequestValidator(), TimeProvider.System);

    private static async Task<DomainMonitor> SeedMonitorAsync(
        ApplicationDbContext db, string userId = "user-1", string? name = null)
    {
        var monitor = new DomainMonitor
        {
            UserId = userId,
            Name = name ?? $"Site-{Guid.NewGuid():N}",
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
    public async Task CreateAssignsMonitorsInOrder()
    {
        await using var provider = await CreateServicesAsync(database);

        Guid first = Guid.Empty, second = Guid.Empty;
        await UseScopeAsync(provider, async db =>
        {
            first = (await SeedMonitorAsync(db, "user-1", "Alpha")).Id;
            second = (await SeedMonitorAsync(db, "user-1", "Beta")).Id;
        });

        await UseScopeAsync(provider, async db =>
        {
            var created = await CreateService(db).CreateAsync(
                "user-1", new CreateStatusPageRequest("Status", "AcMe-Status", true, [second, first]));

            Assert.Equal("acme-status", created.Slug);
            Assert.Equal([second, first], created.Monitors.Select(m => m.MonitorId));
            Assert.Equal(["Beta", "Alpha"], created.Monitors.Select(m => m.MonitorName));
        });
    }

    [Fact]
    public async Task DuplicateSlugAutoSuffixesAndForeignMonitorsRejected()
    {
        await using var provider = await CreateServicesAsync(database);

        Guid owned = Guid.Empty, foreign = Guid.Empty;
        await UseScopeAsync(provider, async db =>
        {
            owned = (await SeedMonitorAsync(db, "user-1")).Id;
            foreign = (await SeedMonitorAsync(db, "user-2")).Id;
            await CreateService(db).CreateAsync(
                "user-1", new CreateStatusPageRequest("First", "taken", true, [owned]));
        });

        await UseScopeAsync(provider, async db =>
        {
            var service = CreateService(db);
            var suffixed = await service.CreateAsync(
                "user-2", new CreateStatusPageRequest("Second", "TAKEN", true, [foreign]));

            Assert.Equal("taken-2", suffixed.Slug);

            await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
                "user-1", new CreateStatusPageRequest("Second", "free", true, [foreign])));
            await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
                "user-1", new CreateStatusPageRequest("Second", "free", true, [Guid.NewGuid()])));
        });
    }

    [Fact]
    public async Task SlugGeneratedFromNameWhenBlank()
    {
        await using var provider = await CreateServicesAsync(database);

        await UseScopeAsync(provider, async db =>
        {
            var monitor = await SeedMonitorAsync(db);
            var service = CreateService(db);
            var first = await service.CreateAsync(
                "user-1", new CreateStatusPageRequest("Acme Status!", null, true, [monitor.Id]));
            var second = await service.CreateAsync(
                "user-1", new CreateStatusPageRequest("Acme Status!", "", true, [monitor.Id]));

            Assert.Equal("acme-status", first.Slug);
            Assert.Equal("acme-status-2", second.Slug);
        });
    }

    [Fact]
    public async Task CheckSlugReportsAvailabilityAndSuggestion()
    {
        await using var provider = await CreateServicesAsync(database);

        Guid id = Guid.Empty;
        await UseScopeAsync(provider, async db =>
        {
            var monitor = await SeedMonitorAsync(db);
            id = (await CreateService(db).CreateAsync(
                "user-1", new CreateStatusPageRequest("Acme", "acme", true, [monitor.Id]))).StatusPageId;
        });

        await UseScopeAsync(provider, async db =>
        {
            var service = CreateService(db);

            var free = await service.CheckSlugAsync("brand-new", null, null);
            Assert.True(free.Available);
            Assert.Equal("brand-new", free.Slug);
            Assert.Equal("brand-new", free.Suggestion);

            var taken = await service.CheckSlugAsync("ACME!!", null, null);
            Assert.False(taken.Available);
            Assert.Equal("acme", taken.Slug);
            Assert.Equal("acme-2", taken.Suggestion);

            var own = await service.CheckSlugAsync("acme", null, id);
            Assert.True(own.Available);
        });
    }

    [Fact]
    public async Task ListAndGetHonorOwnership()
    {
        await using var provider = await CreateServicesAsync(database);

        Guid id = Guid.Empty;
        await UseScopeAsync(provider, async db =>
        {
            var monitor = await SeedMonitorAsync(db);
            id = (await CreateService(db).CreateAsync(
                "user-1", new CreateStatusPageRequest("B Page", "b-page", true, [monitor.Id]))).StatusPageId;
            await CreateService(db).CreateAsync(
                "user-1", new CreateStatusPageRequest("A Page", "a-page", false, [monitor.Id]));
        });

        await UseScopeAsync(provider, async db =>
        {
            var service = CreateService(db);
            var pages = await service.ListAsync("user-1");

            Assert.Equal(["A Page", "B Page"], pages.Select(p => p.Name));
            Assert.Empty(await service.ListAsync("user-2"));
            Assert.NotNull(await service.GetByIdAsync("user-1", id));
            Assert.Null(await service.GetByIdAsync("user-2", id));
            Assert.Null(await service.GetByIdAsync("user-1", Guid.NewGuid()));
        });
    }

    [Fact]
    public async Task UpdateReplacesFieldsAndMonitorSet()
    {
        await using var provider = await CreateServicesAsync(database);

        Guid id = Guid.Empty, replacement = Guid.Empty;
        await UseScopeAsync(provider, async db =>
        {
            var monitor = await SeedMonitorAsync(db);
            replacement = (await SeedMonitorAsync(db)).Id;
            id = (await CreateService(db).CreateAsync(
                "user-1", new CreateStatusPageRequest("Old", "old", true, [monitor.Id]))).StatusPageId;
            await CreateService(db).CreateAsync(
                "user-1", new CreateStatusPageRequest("Other", "other", true, [monitor.Id]));
        });

        await UseScopeAsync(provider, async db =>
        {
            var service = CreateService(db);
            var updated = await service.UpdateAsync(
                "user-1",
                id,
                new UpdateStatusPageRequest("New", "new-slug", false, [replacement]));

            Assert.NotNull(updated);
            Assert.Equal("New", updated.Name);
            Assert.Equal("new-slug", updated.Slug);
            Assert.False(updated.IsPublic);
            Assert.Equal([replacement], updated.Monitors.Select(m => m.MonitorId));

            var reslugged = await service.UpdateAsync(
                "user-1", id, new UpdateStatusPageRequest(null, "other", null, null));

            Assert.NotNull(reslugged);
            Assert.Equal("other-2", reslugged.Slug);
            await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(
                "user-1", id, new UpdateStatusPageRequest(null, null, null, null)));
            Assert.Null(await service.UpdateAsync(
                "user-2", id, new UpdateStatusPageRequest("X", null, null, null)));
        });
    }

    [Fact]
    public async Task DeleteRemovesOwnedPageOnly()
    {
        await using var provider = await CreateServicesAsync(database);

        Guid id = Guid.Empty;
        await UseScopeAsync(provider, async db =>
        {
            var monitor = await SeedMonitorAsync(db);
            id = (await CreateService(db).CreateAsync(
                "user-1", new CreateStatusPageRequest("Old", "old", true, [monitor.Id]))).StatusPageId;
        });

        await UseScopeAsync(provider, async db =>
        {
            var service = CreateService(db);
            Assert.False(await service.DeleteAsync("user-2", id));
            Assert.True(await service.DeleteAsync("user-1", id));
            Assert.Null(await service.GetByIdAsync("user-1", id));
        });
    }

    [Fact]
    public async Task PublicReadExposesPublishedPageOnly()
    {
        await using var provider = await CreateServicesAsync(database);

        await UseScopeAsync(provider, async db =>
        {
            var monitor = await SeedMonitorAsync(db, "user-1", "Site");
            db.MonitorChecks.Add(new MonitorCheck
            {
                Id = Guid.NewGuid(),
                MonitorId = monitor.Id,
                Timestamp = DateTimeOffset.UtcNow,
                IsUp = true,
                StatusCode = 200,
                ResponseTimeMs = 100,
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            await service.CreateAsync("user-1", new CreateStatusPageRequest("Pub", "pub", true, [monitor.Id]));
            await service.CreateAsync("user-1", new CreateStatusPageRequest("Priv", "priv", false, [monitor.Id]));
        });

        await UseScopeAsync(provider, async db =>
        {
            var service = CreateService(db);
            var page = await service.GetPublicAsync("PUB");

            Assert.NotNull(page);
            Assert.Equal("Pub", page.Name);
            var single = Assert.Single(page.Monitors);
            Assert.Equal("Site", single.Name);
            Assert.Equal("https://example.com", single.Url);
            Assert.Equal(ProtocolType.Http, single.Protocol);
            Assert.Equal(100, single.UptimePercentage);

            Assert.Null(await service.GetPublicAsync("priv"));
            Assert.Null(await service.GetPublicAsync("missing"));
        });
    }

    [Fact]
    public async Task PublicReadIncludesHourlyAndDailySeries()
    {
        await using var provider = await CreateServicesAsync(database);
        var now = DateTimeOffset.UtcNow;
        var hour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);

        await UseScopeAsync(provider, async db =>
        {
            var monitor = await SeedMonitorAsync(db, "user-1", "Site");
            db.HourlyUptimeSummaries.Add(new HourlyUptimeSummary
            {
                MonitorId = monitor.Id, Hour = hour.AddHours(-1),
                TotalChecks = 2, UpCount = 1, AverageResponseTimeMs = 50,
            });
            db.DailyUptimeSummaries.Add(new DailyUptimeSummary
            {
                MonitorId = monitor.Id, Date = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1),
                TotalChecks = 4, UpCount = 4, AverageResponseTimeMs = 60,
            });
            await db.SaveChangesAsync();
            await CreateService(db).CreateAsync(
                "user-1", new CreateStatusPageRequest("Pub", "pub", true, [monitor.Id]));
        });

        await UseScopeAsync(provider, async db =>
        {
            var page = await CreateService(db).GetPublicAsync("pub");

            Assert.NotNull(page);
            var single = Assert.Single(page.Monitors);
            var hourly = Assert.Single(single.Hourly);
            Assert.Equal(50, hourly.UptimePercentage);
            var daily = Assert.Single(single.Daily);
            Assert.Equal(100, daily.UptimePercentage);
        });
    }

    [Theory]
    [InlineData("acme-status", true)]
    [InlineData("Acme-Status", false)]
    [InlineData("UPPER", false)]
    [InlineData("has space", false)]
    [InlineData("trailing-", false)]
    [InlineData("-leading", false)]
    [InlineData("double--dash", false)]
    public void SlugShapeValidated(string slug, bool expectedValid)
    {
        var validator = new CreateStatusPageRequestValidator();
        var request = new CreateStatusPageRequest("Page", slug, true, [Guid.NewGuid()]);

        Assert.Equal(expectedValid, validator.Validate(request).IsValid);
    }
}
