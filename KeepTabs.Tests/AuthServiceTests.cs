using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using KeepTabs.Application.Users;
using KeepTabs.Application.Users.Dtos;
using KeepTabs.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace KeepTabs.Tests;

public sealed class AuthServiceTests
{
    private static (AuthService Service, StubUserAccountStore Store) CreateService(bool failCreate = false)
    {
        var store = new StubUserAccountStore { FailCreate = failCreate };
        var service = new AuthService(store, new StubJwtTokenService(), new ApiKeyService());

        return (service, store);
    }

    [Fact]
    public async Task LoginReturnsTokenForValidCredentials()
    {
        var (service, store) = CreateService();
        store.Seed("user-1", "Ada@Example.com", "secret", "Ada", "Lovelace");

        var response = await service.LoginAsync(new LoginRequest("ada@example.com", "secret"));

        Assert.NotNull(response);
        Assert.Equal("TOKEN:user-1", response.Token);
        Assert.Equal("user-1", response.UserId);
        Assert.Equal("Ada@Example.com", response.Email);
        Assert.Equal("Ada", response.FirstName);
    }

    [Fact]
    public async Task LoginReturnsNullForUnknownEmail()
    {
        var (service, _) = CreateService();

        Assert.Null(await service.LoginAsync(new LoginRequest("nobody@example.com", "secret")));
    }

    [Fact]
    public async Task LoginReturnsNullForWrongPassword()
    {
        var (service, store) = CreateService();
        store.Seed("user-1", "ada@example.com", "secret", null, null);

        Assert.Null(await service.LoginAsync(new LoginRequest("ada@example.com", "wrong")));
    }

    [Fact]
    public async Task RegisterCreatesAccountAndReturnsToken()
    {
        var (service, store) = CreateService();

        var (response, errors) = await service.RegisterAsync(
            new RegisterRequest("  new@example.com ", "password123", "Grace", null));

        Assert.Empty(errors);
        Assert.NotNull(response);
        Assert.StartsWith("TOKEN:", response.Token);
        Assert.Equal("new@example.com", store.ByEmail("new@example.com")?.Email);
    }

    [Fact]
    public async Task RegisterFailurePropagatesErrors()
    {
        var (service, _) = CreateService(failCreate: true);

        var (response, errors) = await service.RegisterAsync(
            new RegisterRequest("taken@example.com", "password123", null, null));

        Assert.Null(response);
        Assert.Equal(["Email already taken."], errors);
    }

    [Fact]
    public async Task RegenerateApiKeyStoresHashAndReturnsRawKeyOnce()
    {
        var (service, store) = CreateService();
        store.Seed("user-1", "ada@example.com", "secret", null, null);
        var apiKeys = new ApiKeyService();

        var response = await service.RegenerateApiKeyAsync("user-1");

        Assert.NotNull(response);
        Assert.Equal(apiKeys.ComputeHash(response.ApiKey), store.ApiKeyHashes["user-1"]);
    }

    [Fact]
    public async Task RegenerateApiKeyForUnknownUserReturnsNull()
    {
        var (service, _) = CreateService();

        Assert.Null(await service.RegenerateApiKeyAsync("missing"));
    }

    [Fact]
    public async Task UserServiceReturnsProfileAndNullForUnknown()
    {
        var (_, store) = CreateService();
        store.Seed("user-1", "ada@example.com", "secret", "Ada", null);
        var service = new UserService(store);

        var profile = await service.GetUserByIdAsync("user-1");

        Assert.NotNull(profile);
        Assert.Equal("ada@example.com", profile.Email);
        Assert.Equal("Ada", profile.FirstName);
        Assert.Null(await service.GetUserByIdAsync("missing"));
    }

    [Fact]
    public void JwtTokenServiceIssuesParseableTokenWithExpectedClaims()
    {
        var options = Options.Create(new JwtOptions
        {
            Key = "0123456789abcdef0123456789abcdef",
            Issuer = "keeptabs-test",
            Audience = "keeptabs-test",
            ExpiryMinutes = 60,
        });
        var service = new JwtTokenService(options, TimeProvider.System);
        var account = new UserAccount("user-1", "ada@example.com", "Ada", null, ["Admin"]);

        var raw = service.GenerateToken(account);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(raw);

        Assert.Equal("user-1", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("ada@example.com", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Ada", token.Claims.First(c => c.Type == ClaimTypes.GivenName).Value);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        Assert.Equal("keeptabs-test", token.Issuer);
        Assert.InRange(token.ValidTo, DateTime.UtcNow.AddMinutes(55), DateTime.UtcNow.AddMinutes(65));
    }

    [Fact]
    public void ApiKeysAreUrlSafeAndHashesAreStable()
    {
        var service = new ApiKeyService();
        var rawApiKey = service.GenerateRawApiKey();

        Assert.Equal(43, rawApiKey.Length);
        Assert.Equal(rawApiKey, rawApiKey.TrimEnd('='));
        Assert.Equal(44, service.ComputeHash(rawApiKey).Length);
        Assert.Equal(service.ComputeHash(rawApiKey), service.ComputeHash(rawApiKey));
        Assert.NotEqual(service.ComputeHash(rawApiKey), service.ComputeHash(service.GenerateRawApiKey()));
    }

    [Fact]
    public void RegistrationRejectsWeakCredentials()
    {
        var validator = new RegisterRequestValidator();
        var request = new RegisterRequest("not-an-email", "short", null, null);

        Assert.False(validator.Validate(request).IsValid);
    }

    private sealed class StubJwtTokenService : IJwtTokenService
    {
        public string GenerateToken(UserAccount user) => $"TOKEN:{user.Id}";
    }

    private sealed class StubUserAccountStore : IUserAccountStore
    {
        private readonly Dictionary<string, UserAccount> _byId = new();
        private readonly Dictionary<string, string> _passwords = new();

        public Dictionary<string, string?> ApiKeyHashes { get; } = new();
        public bool FailCreate { get; set; }

        public void Seed(string id, string email, string password, string? firstName, string? lastName)
        {
            _byId[id] = new UserAccount(id, email, firstName, lastName, []);
            _passwords[id] = password;
        }

        public UserAccount? ByEmail(string email) =>
            _byId.Values.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        public Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(ByEmail(email));

        public Task<UserAccount?> FindByIdAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.GetValueOrDefault(userId));

        public Task<UserAccount?> FindByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.Values.FirstOrDefault(u =>
                ApiKeyHashes.TryGetValue(u.Id, out var hash) && hash == apiKeyHash));

        public Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.ContainsKey(userId));

        public Task<bool> CheckPasswordAsync(string userId, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(_passwords.GetValueOrDefault(userId) == password);

        public Task<AccountCreationResult> CreateAsync(
            string email, string password, string? firstName, string? lastName,
            CancellationToken cancellationToken = default)
        {
            if (FailCreate)
            {
                return Task.FromResult(new AccountCreationResult(false, null, ["Email already taken."]));
            }

            var id = Guid.NewGuid().ToString();
            Seed(id, email, password, firstName, lastName);

            return Task.FromResult(new AccountCreationResult(true, id, []));
        }

        public Task<bool> UpdateApiKeyHashAsync(string userId, string? apiKeyHash, CancellationToken cancellationToken = default)
        {
            if (!_byId.ContainsKey(userId))
            {
                return Task.FromResult(false);
            }

            ApiKeyHashes[userId] = apiKeyHash;

            return Task.FromResult(true);
        }
    }
}
