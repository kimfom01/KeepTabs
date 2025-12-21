using KeepTabs.Application.Users.Dtos;

namespace KeepTabs.Application.Users;

public interface IUserService
{
    Task<GetUserResponse?> GetUsers(string userId, CancellationToken cancellationToken = default);
    IEnumerable<GetUserResponse> GetUsers();
}