using Sergin.SharedKernel.Application.Commands.Queries;
using Sergin.SharedKernel.Application.Securities.Authorization;

namespace Sergin.UserAccess.Application.Users.Commands.GetOne;

[RequiredPermissions("permission.ua.users.read")]
public sealed record GetUserByIdQueryCommand(Guid Id) : IQuery<UserQueryResponse>;
