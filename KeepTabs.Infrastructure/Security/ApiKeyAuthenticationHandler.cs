using System.Security.Claims;
using System.Text.Encodings.Web;
using KeepTabs.Application.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeepTabs.Infrastructure.Security;

/// <summary>
/// Authentication-scheme names for API keys.
/// </summary>
public static class ApiKeyAuthenticationDefaults
{
    public const string AuthenticationScheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";
}

/// <summary>
/// Authenticates requests with a hashed API key supplied in a dedicated header.
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IUserAccountStore _userAccounts;
    private readonly IApiKeyService _apiKeys;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IUserAccountStore userAccounts,
        IApiKeyService apiKeys)
        : base(options, logger, encoder)
    {
        _userAccounts = userAccounts;
        _apiKeys = apiKeys;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var values))
        {
            return AuthenticateResult.NoResult();
        }

        var rawApiKey = values.ToString();
        if (string.IsNullOrWhiteSpace(rawApiKey) || rawApiKey.Length is < 20 or > 256)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        var user = await _userAccounts.FindByApiKeyHashAsync(_apiKeys.ComputeHash(rawApiKey), Context.RequestAborted);
        if (user is null)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.AuthenticationMethod, "api_key")
        };

        if (user.FirstName is not null)
        {
            claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
        }

        if (user.LastName is not null)
        {
            claims.Add(new Claim(ClaimTypes.Surname, user.LastName));
        }

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
