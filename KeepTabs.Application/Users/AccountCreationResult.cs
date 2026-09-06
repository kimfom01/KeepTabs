namespace KeepTabs.Application.Users;

/// <summary>
/// Result of creating a user without exposing Identity infrastructure types.
/// </summary>
public sealed record AccountCreationResult(
    bool Succeeded,
    string? UserId,
    IReadOnlyList<string> Errors);
