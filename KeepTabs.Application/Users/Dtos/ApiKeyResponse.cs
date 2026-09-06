namespace KeepTabs.Application.Users.Dtos;

/// <summary>One-time API-key response. Store the raw key securely; it cannot be retrieved again.</summary>
public sealed record ApiKeyResponse(
    string ApiKey);
