using Microsoft.EntityFrameworkCore;
using Sergin.UserAccess.Domain.Users;
using Sergin.UserAccess.Infrastructure.Data;

namespace Sergin.UserAccess.Infrastructure.Users.Repositories;

internal class UserRepository(IUserAccessDbContext dbContext) : IUserRepository
{
    public ValueTask<User?> GetAsync(UserInternalId id, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<User>().FindAsync([id, cancellationToken], cancellationToken: cancellationToken);
    }

    public Task<User?> GetByUserName(UserName userName, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<User>().SingleOrDefaultAsync(u => u.UserName == userName, cancellationToken);
    }

    public void Insert(User entity)
    {
        dbContext.Set<User>().Add(entity);
    }

    public void Remove(User entity)
    {
        dbContext.Set<User>().Remove(entity);
    }
}
