namespace KeepTabs.Application.Users.Dtos;

/// <summary>JWT authentication response.</summary>
public sealed record AuthResponse(
    string Token,
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    IReadOnlyList<string> Roles);
