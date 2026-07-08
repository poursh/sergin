using Sergin.SharedKernel.Application.Commands.Queries;

namespace Sergin.UserAccess.Application.Users.Commands.GetList;
public interface IGetUserListQueryRepository
{
    Task<ListQueryResponse<GetUserListItem>> GetListAsync(ListQuery<GetUserListItem> query, CancellationToken cancellationToken = default);
}
