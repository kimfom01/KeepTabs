namespace KeepTabs.Application.Users.Dtos;

/// <summary>Public user profile response.</summary>
public sealed record GetUserResponse(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName);
