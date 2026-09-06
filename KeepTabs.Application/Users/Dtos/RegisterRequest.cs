namespace KeepTabs.Application.Users.Dtos;

/// <summary>Payload for creating a user account.</summary>
public sealed record RegisterRequest(
    string Email,
    string Password,
    string? FirstName,
    string? LastName);
