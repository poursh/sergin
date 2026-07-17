using Sergin.SharedKernel.Application.Commands;

namespace Sergin.UserAccess.Application.Users.Commands.DeactivateUser;

public sealed record DeactivateUserCommand(Guid Id) : ICommand<DeactivateUserCommandResponse>;
