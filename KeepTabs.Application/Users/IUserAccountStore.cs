namespace KeepTabs.Application.Users;

/// <summary>
/// Persistence boundary for user accounts.
/// </summary>
public interface IUserAccountStore
{
    Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserAccount?> FindByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserAccount?> FindByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> CheckPasswordAsync(string userId, string password, CancellationToken cancellationToken = default);
    Task<AccountCreationResult> CreateAsync(
        string email,
        string password,
        string? firstName,
        string? lastName,
        CancellationToken cancellationToken = default);
    Task<bool> UpdateApiKeyHashAsync(string userId, string? apiKeyHash, CancellationToken cancellationToken = default);
}
