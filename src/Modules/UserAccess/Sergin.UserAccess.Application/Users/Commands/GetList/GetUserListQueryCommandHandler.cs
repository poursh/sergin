using ErrorOr;
using Sergin.SharedKernel.Application;
using Sergin.SharedKernel.Application.Commands.Queries;

namespace Sergin.UserAccess.Application.Users.Commands.GetList;

internal sealed class GetUserListQueryCommandHandler(IGetUserListQueryRepository queryRepository) : IListQueryHandler<GetUserListItem>
{
    public async Task<ErrorOr<ListQueryResponse<GetUserListItem>>> Handle(
        ListQuery<GetUserListItem> request, CancellationToken cancellationToken)
    {
        ListQueryResponse<GetUserListItem> res = await queryRepository.GetListAsync(request, cancellationToken);

        return res;
    }
}
