using Sergin.UserAccess.Domain.Users;
using Sergin.SharedKernel.Application.Commands.Queries;

namespace Sergin.UserAccess.Application.Users.Commands.GetOne;

internal sealed class GetUserByIdQueryCommandHandler(IGetUserQueryRepository repository) : IQueryHandler<GetUserByIdQueryCommand, UserQueryResponse>
{
    public async Task<ErrorOr<UserQueryResponse>> Handle(GetUserByIdQueryCommand request, CancellationToken cancellationToken)
    {
        UserQueryResponse? res = await repository.GetUserById(new UserInternalId(request.Id), cancellationToken);

        if (res is null)
        {
            return Error.NotFound();
        }

        return res;
    }
}
