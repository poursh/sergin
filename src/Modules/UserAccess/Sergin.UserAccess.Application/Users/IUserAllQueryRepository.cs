using Sergin.UserAccess.Application.Users.Commands.GetList;
using Sergin.UserAccess.Application.Users.Commands.GetOne;

namespace Sergin.UserAccess.Application.Users;
public interface IUserAllQueryRepository : IGetUserListQueryRepository, IGetUserQueryRepository;
