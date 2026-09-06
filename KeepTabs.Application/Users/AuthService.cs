using KeepTabs.Application.Users.Dtos;

namespace KeepTabs.Application.Users;

/// <summary>
/// Orchestrates registration, password authentication, and API-key rotation.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IUserAccountStore _userAccounts;
    private readonly IJwtTokenService _tokens;
    private readonly IApiKeyService _apiKeys;

    public AuthService(IUserAccountStore userAccounts, IJwtTokenService tokens, IApiKeyService apiKeys)
    {
        _userAccounts = userAccounts;
        _tokens = tokens;
        _apiKeys = apiKeys;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userAccounts.FindByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null)
        {
            return null;
        }

        var passwordValid = await _userAccounts.CheckPasswordAsync(user.Id, request.Password, cancellationToken);
        if (!passwordValid)
        {
            return null;
        }

        return new AuthResponse(
            _tokens.GenerateToken(user),
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Roles);
    }

    public async Task<(AuthResponse? Response, IReadOnlyList<string> Errors)> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _userAccounts.CreateAsync(
            request.Email.Trim(),
            request.Password,
            request.FirstName,
            request.LastName,
            cancellationToken);

        if (!result.Succeeded || result.UserId is null)
        {
            return (null, result.Errors);
        }

        var user = await _userAccounts.FindByIdAsync(result.UserId, cancellationToken);
        if (user is null)
        {
            return (null, ["User was created, but the created account could not be loaded."]);
        }

        return (new AuthResponse(
            _tokens.GenerateToken(user),
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Roles), []);
    }

    public async Task<ApiKeyResponse?> RegenerateApiKeyAsync(string userId, CancellationToken cancellationToken = default)
    {
        var rawApiKey = _apiKeys.GenerateRawApiKey();
        var updated = await _userAccounts.UpdateApiKeyHashAsync(userId, _apiKeys.ComputeHash(rawApiKey), cancellationToken);

        return updated ? new ApiKeyResponse(rawApiKey) : null;
    }
}
