namespace KeepTabs.Application.Users.Dtos;

/// <summary>Credentials for password authentication.</summary>
public sealed record LoginRequest(
    string Email,
    string Password);
