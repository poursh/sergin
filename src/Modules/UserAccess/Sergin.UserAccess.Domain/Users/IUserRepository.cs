using Sergin.SharedKernel.Domain.Repositories;

namespace Sergin.UserAccess.Domain.Users;
public interface IUserRepository : IRepository<User, UserInternalId>
{
    Task<User?> GetByUserName(UserName userName, CancellationToken cancellationToken = default);
}
