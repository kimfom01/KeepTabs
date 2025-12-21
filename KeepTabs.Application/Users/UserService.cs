using KeepTabs.Application.Users.Dtos;
using KeepTabs.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace KeepTabs.Application.Users;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _dbContext;

    public UserService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetUserResponse?> GetUsers(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);

        return user?.ToResponse();
    }

    public IEnumerable<GetUserResponse> GetUsers()
    {
        var users = _dbContext.Users.AsNoTracking();

        return users.Select(u => u.ToResponse());
    }
}