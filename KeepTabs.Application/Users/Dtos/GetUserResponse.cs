namespace KeepTabs.Application.Users.Dtos;

public class GetUserResponse
{
    public required string UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ApiKey { get; set; }
}