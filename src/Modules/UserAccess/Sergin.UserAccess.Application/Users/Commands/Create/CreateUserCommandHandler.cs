using Sergin.UserAccess.Domain.Users;
using Sergin.SharedKernel.Application.Commands;

namespace Sergin.UserAccess.Application.Users.Commands.Create;

internal sealed class CreateUserCommandHandler(
    IUserAccessUnitOfWork unitOfWork,
    IUserRepository repository) : ICommandHandler<CreateUserCommand, CreateUserCommandResponse>
{
    public async Task<ErrorOr<CreateUserCommandResponse>> Handle(
        CreateUserCommand request, CancellationToken cancellationToken)
    {
        var newUser = User.Create(request.UserName);

        repository.Insert(newUser);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateUserCommandResponse(newUser.Id.Value);
    }
}
