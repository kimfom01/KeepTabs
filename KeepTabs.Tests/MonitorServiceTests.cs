using FluentValidation;
using KeepTabs.Application.Common.Interfaces;
using KeepTabs.Application.Monitors;
using KeepTabs.Application.Monitors.Dtos;
using KeepTabs.Application.Users;
using KeepTabs.Domain;
using KeepTabs.Infrastructure;
using KeepTabs.Infrastructure.Database;
using KeepTabs.Tests.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainMonitor = KeepTabs.Domain.Monitor;

namespace KeepTabs.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class MonitorServiceTests(PostgresFixture database)
{
    private async Task<(ServiceProvider Provider, FakeUserStore Users)> CreateServicesAsync(
        params string[] userIds)
    {
        var connectionString = await database.CreateDatabaseAsync();
        var users = new FakeUserStore(userIds);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMonitorCheckingInfrastructure();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        var provider = services.BuildServiceProvider();

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        return (provider, users);
    }

    private static async Task<DomainMonitor> SeedMonitorAsync(
        IApplicationDbContext db, string userId = "user-1", string name = "Site")
    {
        var monitor = new DomainMonitor
        {
            UserId = userId,
            Name = name,
            Url = "https://example.com",
            Protocol = ProtocolType.Http,
            CheckIntervalSeconds = 60,
            TimeoutSeconds = 10,
            ExpectedStatusCode = 200,
        };
        db.Monitors.Add(monitor);
        await db.SaveChangesAsync();

        return monitor;
    }

    private static MonitorService CreateService(ApplicationDbContext db, FakeUserStore users) =>
        new(db, users, new CreateMonitorRequestValidator(), TimeProvider.System);

    private static async Task UseScopeAsync(
        ServiceProvider provider, Func<ApplicationDbContext, FakeUserStore, Task> action, FakeUserStore users)
    {
        await using var scope = provider.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), users);
    }

    [Fact]
    public async Task CreateAssignsOwnerAndPersists()
    {
        var (provider, users) = await CreateServicesAsync("user-1");
        await using var _ = provider;

        GetMonitorResponse created = null!;
        await UseScopeAsync(provider, async (db, store) =>
        {
            created = await CreateService(db, store).CreateMonitorAsync(
                "user-1",
                new CreateMonitorRequest("Site", "https://example.com", ProtocolType.Http, 60, 10, 200));
        }, users);

        Assert.Equal("user-1", created.UserId);
        Assert.Equal("Site", created.Name);
        Assert.False(created.IsPaused);
        Assert.NotEqual(Guid.Empty, created.MonitorId);
    }

    [Fact]
    public async Task CreateRejectsUnknownUser()
    {
        var (provider, users) = await CreateServicesAsync("user-1");
        await using var _ = provider;

        await UseScopeAsync(provider, async (db, store) =>
        {
            await Assert.ThrowsAsync<ValidationException>(() => CreateService(db, store).CreateMonitorAsync(
                "ghost",
                new CreateMonitorRequest("Site", "https://example.com", ProtocolType.Http, 60, 10, 200)));
        }, users);
    }

    [Fact]
    public async Task DuplicateNameRejectedForSameUserButAllowedAcrossUsers()
    {
        var (provider, users) = await CreateServicesAsync("user-1", "user-2");
        await using var _ = provider;

        await UseScopeAsync(provider, async (db, store) =>
        {
            var service = CreateService(db, store);
            await service.CreateMonitorAsync(
                "user-1", new CreateMonitorRequest("Site", "https://example.com", ProtocolType.Http, 60, 10, 200));

            await Assert.ThrowsAsync<ValidationException>(() => service.CreateMonitorAsync(
                "user-1", new CreateMonitorRequest("Site", "https://other.example.com", ProtocolType.Http, 60, 10, 200)));

            var other = await service.CreateMonitorAsync(
                "user-2", new CreateMonitorRequest("Site", "https://example.com", ProtocolType.Http, 60, 10, 200));
            Assert.Equal("user-2", other.UserId);
        }, users);
    }

    [Fact]
    public async Task ListScopedToOwnerAndOrderedByName()
    {
        var (provider, users) = await CreateServicesAsync("user-1", "user-2");
        await using var _ = provider;

        await UseScopeAsync(provider, async (db, _) =>
        {
            await SeedMonitorAsync(db, "user-1", "Bravo");
            await SeedMonitorAsync(db, "user-1", "Alpha");
            await SeedMonitorAsync(db, "user-2", "Other");
        }, users);

        await UseScopeAsync(provider, async (db, store) =>
        {
            var monitors = await CreateService(db, store).GetMonitorsAsync("user-1");

            Assert.Equal(["Alpha", "Bravo"], monitors.Select(m => m.Name));
        }, users);
    }

    [Fact]
    public async Task CreatePersistsHeadOptionAndUpdateMergesIt()
    {
        var (provider, users) = await CreateServicesAsync("user-1");
        await using var _ = provider;

        Guid id = Guid.Empty;
        await UseScopeAsync(provider, async (db, store) =>
        {
            var created = await CreateService(db, store).CreateMonitorAsync(
                "user-1",
                new CreateMonitorRequest("Site", "https://example.com", ProtocolType.Http, 60, 10, 200, true));
            id = created.MonitorId;

            Assert.True(created.UseHeadRequest);
        }, users);

        await UseScopeAsync(provider, async (db, store) =>
        {
            var updated = await CreateService(db, store).UpdateMonitorAsync(
                "user-1", id, new UpdateMonitorRequest(null, null, null, null, null, null, null, false));

            Assert.NotNull(updated);
            Assert.False(updated.UseHeadRequest);
        }, users);
    }

    [Fact]
    public async Task GetByIdHonorsOwnership()
    {
        var (provider, users) = await CreateServicesAsync("user-1", "user-2");
        await using var _ = provider;

        Guid id = Guid.Empty;
        await UseScopeAsync(provider, async (db, _) => { id = (await SeedMonitorAsync(db)).Id; }, users);

        await UseScopeAsync(provider, async (db, store) =>
        {
            var service = CreateService(db, store);
            Assert.NotNull(await service.GetMonitorByIdAsync("user-1", id));
            Assert.Null(await service.GetMonitorByIdAsync("user-2", id));
            Assert.Null(await service.GetMonitorByIdAsync("user-1", Guid.NewGuid()));
        }, users);
    }

    [Fact]
    public async Task UpdateAppliesPartialChanges()
    {
        var (provider, users) = await CreateServicesAsync("user-1");
        await using var _ = provider;

        Guid id = Guid.Empty;
        await UseScopeAsync(provider, async (db, _) => { id = (await SeedMonitorAsync(db)).Id; }, users);

        await UseScopeAsync(provider, async (db, store) =>
        {
            var updated = await CreateService(db, store).UpdateMonitorAsync(
                "user-1", id, new UpdateMonitorRequest("Renamed", null, null, null, null, null, true));

            Assert.NotNull(updated);
            Assert.Equal("Renamed", updated.Name);
            Assert.True(updated.IsPaused);
            Assert.Equal("https://example.com", updated.Url);
        }, users);
    }

    [Fact]
    public async Task UpdateWithNoFieldsThrows()
    {
        var (provider, users) = await CreateServicesAsync("user-1");
        await using var _ = provider;

        Guid id = Guid.Empty;
        await UseScopeAsync(provider, async (db, _) => { id = (await SeedMonitorAsync(db)).Id; }, users);

        await UseScopeAsync(provider, async (db, store) =>
        {
            await Assert.ThrowsAsync<ValidationException>(() => CreateService(db, store).UpdateMonitorAsync(
                "user-1", id, new UpdateMonitorRequest(null, null, null, null, null, null, null)));
        }, users);
    }

    [Fact]
    public async Task UpdateRejectsIneffectiveInvalidCombination()
    {
        var (provider, users) = await CreateServicesAsync("user-1");
        await using var _ = provider;

        Guid id = Guid.Empty;
        await UseScopeAsync(provider, async (db, _) => { id = (await SeedMonitorAsync(db)).Id; }, users);

        // Existing interval is 60s; raising only the timeout to 60s breaks timeout < interval.
        await UseScopeAsync(provider, async (db, store) =>
        {
            await Assert.ThrowsAsync<ValidationException>(() => CreateService(db, store).UpdateMonitorAsync(
                "user-1", id, new UpdateMonitorRequest(null, null, null, null, 60, null, null)));
        }, users);
    }

    [Fact]
    public async Task UpdateRejectsDuplicateRename()
    {
        var (provider, users) = await CreateServicesAsync("user-1");
        await using var _ = provider;

        Guid id = Guid.Empty;
        await UseScopeAsync(provider, async (db, _) =>
        {
            await SeedMonitorAsync(db, "user-1", "Alpha");
            id = (await SeedMonitorAsync(db, "user-1", "Bravo")).Id;
        }, users);

        await UseScopeAsync(provider, async (db, store) =>
        {
            await Assert.ThrowsAsync<ValidationException>(() => CreateService(db, store).UpdateMonitorAsync(
                "user-1", id, new UpdateMonitorRequest("Alpha", null, null, null, null, null, null)));
        }, users);
    }

    [Fact]
    public async Task DeleteRemovesOwnedMonitorOnly()
    {
        var (provider, users) = await CreateServicesAsync("user-1", "user-2");
        await using var _ = provider;

        Guid id = Guid.Empty;
        await UseScopeAsync(provider, async (db, _) => { id = (await SeedMonitorAsync(db)).Id; }, users);

        await UseScopeAsync(provider, async (db, store) =>
        {
            var service = CreateService(db, store);
            Assert.False(await service.DeleteMonitorAsync("user-2", id));
            Assert.False(await service.DeleteMonitorAsync("user-1", Guid.NewGuid()));
            Assert.True(await service.DeleteMonitorAsync("user-1", id));
            Assert.Null(await service.GetMonitorByIdAsync("user-1", id));
        }, users);
    }

    [Fact]
    public async Task PauseAndResumeRoundtrip()
    {
        var (provider, users) = await CreateServicesAsync("user-1");
        await using var _ = provider;

        Guid id = Guid.Empty;
        await UseScopeAsync(provider, async (db, _) => { id = (await SeedMonitorAsync(db)).Id; }, users);

        await UseScopeAsync(provider, async (db, store) =>
        {
            var service = CreateService(db, store);
            Assert.True((await service.SetPausedAsync("user-1", id, true))?.IsPaused);
            Assert.False((await service.SetPausedAsync("user-1", id, false))?.IsPaused);
            Assert.Null(await service.SetPausedAsync("user-1", Guid.NewGuid(), true));
        }, users);
    }

    [Fact]
    public async Task SummaryComputesAggregates()
    {
        var (provider, users) = await CreateServicesAsync("user-1");
        await using var _ = provider;

        Guid id = Guid.Empty;
        var now = DateTimeOffset.UtcNow;
        await UseScopeAsync(provider, async (db, _) =>
        {
            id = (await SeedMonitorAsync(db)).Id;
            db.MonitorChecks.AddRange(
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = id, Timestamp = now, IsUp = true,
                    StatusCode = 200, ResponseTimeMs = 100,
                },
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = id, Timestamp = now, IsUp = true,
                    StatusCode = 200, ResponseTimeMs = 200,
                },
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = id, Timestamp = now, IsUp = true,
                    StatusCode = 200, ResponseTimeMs = 300,
                },
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = id, Timestamp = now, IsUp = false,
                    StatusCode = 500, ResponseTimeMs = 400, ErrorMessage = "boom",
                });
            await db.SaveChangesAsync();
        }, users);

        await UseScopeAsync(provider, async (db, store) =>
        {
            var summary = await CreateService(db, store).GetSummaryAsync("user-1", id);

            Assert.NotNull(summary);
            Assert.Equal(4, summary.TotalChecks);
            Assert.Equal(3, summary.UpCount);
            Assert.Equal(1, summary.DownCount);
            Assert.Equal(75, summary.UptimePercentage);
            Assert.Equal(250, summary.AverageResponseTimeMs);
            Assert.Null(await CreateService(db, store).GetSummaryAsync("user-2", id));
        }, users);
    }

    [Fact]
    public async Task SummaryWithoutChecksReturnsZeros()
    {
        var (provider, users) = await CreateServicesAsync("user-1");
        await using var _ = provider;

        Guid id = Guid.Empty;
        await UseScopeAsync(provider, async (db, _) => { id = (await SeedMonitorAsync(db)).Id; }, users);

        await UseScopeAsync(provider, async (db, store) =>
        {
            var summary = await CreateService(db, store).GetSummaryAsync("user-1", id);

            Assert.NotNull(summary);
            Assert.Equal(0, summary.TotalChecks);
            Assert.Equal(0, summary.UptimePercentage);
            Assert.Equal(0, summary.AverageResponseTimeMs);
            Assert.Null(await CreateService(db, store).GetSummaryAsync("user-1", Guid.NewGuid()));
        }, users);
    }

    [Fact]
    public async Task HistoryBoundedOrderedAndScoped()
    {
        var (provider, users) = await CreateServicesAsync("user-1", "user-2");
        await using var _ = provider;

        Guid id = Guid.Empty;
        var now = DateTimeOffset.UtcNow;
        await UseScopeAsync(provider, async (db, _) =>
        {
            id = (await SeedMonitorAsync(db)).Id;
            var other = await SeedMonitorAsync(db, "user-1", "Other");
            db.MonitorChecks.AddRange(
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = id, Timestamp = now.AddDays(-1), IsUp = true,
                    StatusCode = 200, ResponseTimeMs = 10,
                },
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = id, Timestamp = now.AddDays(-10), IsUp = false,
                    StatusCode = null, ResponseTimeMs = 0, ErrorMessage = "old",
                },
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = other.Id, Timestamp = now, IsUp = true,
                    StatusCode = 200, ResponseTimeMs = 10,
                });
            await db.SaveChangesAsync();
        }, users);

        await UseScopeAsync(provider, async (db, store) =>
        {
            var service = CreateService(db, store);
            var week = await service.GetHistoryAsync("user-1", id, 7);

            Assert.Single(week);
            Assert.True(week[0].IsUp);

            var all = await service.GetHistoryAsync("user-1", id, 30);
            Assert.Equal(2, all.Count);
            Assert.True(all[0].Timestamp >= all[1].Timestamp);

            Assert.Empty(await service.GetHistoryAsync("user-2", id, 30));
            Assert.Empty(await service.GetHistoryAsync("user-1", Guid.NewGuid(), 30));
        }, users);
    }

    [Fact]
    public async Task HistoryDaysClampedToSupportedRange()
    {
        var (provider, users) = await CreateServicesAsync("user-1");
        await using var _ = provider;

        Guid id = Guid.Empty;
        var now = DateTimeOffset.UtcNow;
        await UseScopeAsync(provider, async (db, _) =>
        {
            id = (await SeedMonitorAsync(db)).Id;
            db.MonitorChecks.AddRange(
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = id, Timestamp = now.AddDays(-2), IsUp = true,
                    StatusCode = 200, ResponseTimeMs = 10,
                },
                new MonitorCheck
                {
                    Id = Guid.NewGuid(), MonitorId = id, Timestamp = now.AddDays(-400), IsUp = true,
                    StatusCode = 200, ResponseTimeMs = 99,
                });
            await db.SaveChangesAsync();
        }, users);

        await UseScopeAsync(provider, async (db, store) =>
        {
            var service = CreateService(db, store);

            // days=0 clamps to a single day, excluding the 2-day-old check.
            Assert.Empty(await service.GetHistoryAsync("user-1", id, 0));

            // Enormous ranges clamp to a year: the 2-day-old check is included,
            // the 400-day-old check stays excluded.
            var year = await service.GetHistoryAsync("user-1", id, 100_000);
            var single = Assert.Single(year);
            Assert.Equal(10, single.ResponseTimeMs);
        }, users);
    }

    private sealed class FakeUserStore(params string[] userIds) : IUserAccountStore
    {
        private readonly HashSet<string> _known = new(userIds);

        public Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserAccount?>(null);

        public Task<UserAccount?> FindByIdAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserAccount?>(null);

        public Task<UserAccount?> FindByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserAccount?>(null);

        public Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_known.Contains(userId));

        public Task<bool> CheckPasswordAsync(string userId, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<AccountCreationResult> CreateAsync(
            string email, string password, string? firstName, string? lastName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AccountCreationResult(false, null, ["Not supported in tests."]));

        public Task<bool> UpdateApiKeyHashAsync(string userId, string? apiKeyHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
