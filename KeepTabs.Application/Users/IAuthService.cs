using KeepTabs.Application.Users.Dtos;

namespace KeepTabs.Application.Users;

/// <summary>
/// Authentication operations exposed to the API boundary.
/// </summary>
public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<(AuthResponse? Response, IReadOnlyList<string> Errors)> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);
    Task<ApiKeyResponse?> RegenerateApiKeyAsync(string userId, CancellationToken cancellationToken = default);
}
