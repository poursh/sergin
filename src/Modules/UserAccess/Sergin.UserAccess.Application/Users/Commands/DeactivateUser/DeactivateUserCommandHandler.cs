using Sergin.UserAccess.Domain.Users;
using Sergin.SharedKernel.Application.Commands;

namespace Sergin.UserAccess.Application.Users.Commands.DeactivateUser;

internal sealed class DeactivateUserCommandHandler(
    IUserAccessUnitOfWork unitOfWork,
    IUserRepository repository) : ICommandHandler<DeactivateUserCommand, DeactivateUserCommandResponse>
{
    public async Task<ErrorOr<DeactivateUserCommandResponse>> Handle(
        DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        User? user = await repository.GetAsync(new UserInternalId(request.Id), cancellationToken);

        if (user is null)
        {
            return Error.NotFound();
        }

        user.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeactivateUserCommandResponse(user.Id.Value);
    }
}
