namespace KeepTabs.Application.Users;

/// <summary>
/// Generates API keys and their storable hashes. Raw keys are one-time values.
/// </summary>
public interface IApiKeyService
{
    string GenerateRawApiKey();
    string ComputeHash(string rawApiKey);
}
