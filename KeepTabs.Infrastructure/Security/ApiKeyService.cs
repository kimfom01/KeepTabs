using System.Security.Cryptography;
using System.Text;
using KeepTabs.Application.Users;

namespace KeepTabs.Infrastructure.Security;

/// <summary>
/// Creates random API keys and stores only their SHA-256 hashes.
/// </summary>
public sealed class ApiKeyService : IApiKeyService
{
    public string GenerateRawApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string ComputeHash(string rawApiKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawApiKey));

        return Convert.ToBase64String(hash);
    }
}
