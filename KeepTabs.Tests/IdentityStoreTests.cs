using KeepTabs.Application.Users;
using KeepTabs.Infrastructure.Database;
using KeepTabs.Infrastructure.Identity;
using KeepTabs.Tests.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KeepTabs.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class IdentityStoreTests(PostgresFixture database)
{
    private const string Password = "Str0ng!Pass1";

    private async Task<ServiceProvider> CreateServicesAsync()
    {
        var connectionString = await database.CreateDatabaseAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        services
            .AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<IUserAccountStore, AspNetIdentityUserAccountStore>();
        var provider = services.BuildServiceProvider();

        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

        return provider;
    }

    private static async Task UseStoreAsync(ServiceProvider provider, Func<IUserAccountStore, Task> action)
    {
        await using var scope = provider.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<IUserAccountStore>());
    }

    [Fact]
    public async Task CreateAndRoundtripByEmailAndId()
    {
        await using var provider = await CreateServicesAsync();

        await UseStoreAsync(provider, async store =>
        {
            var created = await store.CreateAsync("ada@example.com", Password, "Ada", "Lovelace");

            Assert.True(created.Succeeded);
            Assert.NotNull(created.UserId);
            Assert.Empty(created.Errors);

            var byEmail = await store.FindByEmailAsync("ada@example.com");
            var byId = await store.FindByIdAsync(created.UserId);

            Assert.NotNull(byEmail);
            Assert.Equal(created.UserId, byEmail.Id);
            Assert.Equal("Ada", byEmail.FirstName);
            Assert.Equal("Lovelace", byEmail.LastName);
            Assert.NotNull(byId);
            Assert.Equal(byEmail.Id, byId.Id);
            Assert.Equal(byEmail.Email, byId.Email);
            Assert.Null(await store.FindByEmailAsync("nobody@example.com"));
        });
    }

    [Fact]
    public async Task DuplicateEmailFails()
    {
        await using var provider = await CreateServicesAsync();

        await UseStoreAsync(provider, async store =>
        {
            Assert.True((await store.CreateAsync("ada@example.com", Password, null, null)).Succeeded);

            var duplicate = await store.CreateAsync("ada@example.com", Password, null, null);

            Assert.False(duplicate.Succeeded);
            Assert.Null(duplicate.UserId);
            Assert.NotEmpty(duplicate.Errors);
        });
    }

    [Fact]
    public async Task PasswordVerificationAcceptsCorrectPasswordOnly()
    {
        await using var provider = await CreateServicesAsync();

        await UseStoreAsync(provider, async store =>
        {
            var created = await store.CreateAsync("ada@example.com", Password, null, null);

            Assert.True(await store.CheckPasswordAsync(created.UserId!, Password));
            Assert.False(await store.CheckPasswordAsync(created.UserId!, "wrong-password"));
            Assert.False(await store.CheckPasswordAsync("missing", Password));
        });
    }

    [Fact]
    public async Task ExistsReflectsCreatedUsers()
    {
        await using var provider = await CreateServicesAsync();

        await UseStoreAsync(provider, async store =>
        {
            var created = await store.CreateAsync("ada@example.com", Password, null, null);

            Assert.True(await store.ExistsAsync(created.UserId!));
            Assert.False(await store.ExistsAsync("missing"));
        });
    }

    [Fact]
    public async Task ApiKeyHashRoundtripsThroughLookup()
    {
        await using var provider = await CreateServicesAsync();

        await UseStoreAsync(provider, async store =>
        {
            var created = await store.CreateAsync("ada@example.com", Password, null, null);

            Assert.True(await store.UpdateApiKeyHashAsync(created.UserId!, "hash-1"));
            Assert.Null(await store.FindByApiKeyHashAsync("hash-0"));

            var found = await store.FindByApiKeyHashAsync("hash-1");

            Assert.NotNull(found);
            Assert.Equal(created.UserId, found.Id);
            Assert.False(await store.UpdateApiKeyHashAsync("missing", "hash-1"));
        });
    }
}
