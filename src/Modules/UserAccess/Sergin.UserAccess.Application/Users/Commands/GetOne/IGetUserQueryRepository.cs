using Sergin.UserAccess.Domain.Users;

namespace Sergin.UserAccess.Application.Users.Commands.GetOne;

public interface IGetUserQueryRepository
{
    Task<UserQueryResponse?> GetUserById(UserInternalId Id, CancellationToken cancellationToken = default);
}
