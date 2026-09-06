namespace KeepTabs.Application.Users;

/// <summary>
/// Application-level view of an Identity user, without infrastructure coupling.
/// </summary>
public sealed record UserAccount(
    string Id,
    string Email,
    string? FirstName,
    string? LastName,
    IReadOnlyList<string> Roles);
