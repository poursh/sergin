using System.Data.Common;
using Sergin.UserAccess.Application.Users;
using Sergin.UserAccess.Application.Users.Commands.GetList;
using Sergin.SharedKernel.Application;
using Sergin.SharedKernel.Application.Commands.Queries;
using Sergin.SharedKernel.Infrastracture.Data;
using Sergin.UserAccess.Application.Users.Commands.GetOne;
using Sergin.UserAccess.Domain.Users;

namespace Sergin.UserAccess.Infrastructure.Users.Repositories.Queries;

internal sealed class UserQueryRepository(
    IDbConnectionFactory connectionFactory) : IUserAllQueryRepository
{
    public async Task<UserQueryResponse?> GetUserById(
        UserInternalId Id, CancellationToken cancellationToken = default)
    {
        using DbConnection connection = await connectionFactory.CreateConnectionAsync();

        string queries =
           """
            SELECT id, user_name AS userName
            FROM ua.users
            WHERE id = @Id;
            """;

        return await connection.QuerySingleOrDefaultAsync<UserQueryResponse>(
            queries, new { Id = Id.Value });
    }

    public async Task<ListQueryResponse<GetUserListItem>> GetListAsync(
        ListQuery<GetUserListItem> query, CancellationToken cancellationToken = default)
    {
        using DbConnection connection = await connectionFactory.CreateConnectionAsync();

        string queries =
            """
            SELECT count(*) FROM ua.users;

            SELECT id, user_name AS userName
            FROM ua.users
            LIMIT @PageSize OFFSET @Offset;
            """;

        GridReader res = await connection.QueryMultipleAsync(
            queries, new { PageSize = query.Paggination.Size.Value, Offset = query.Paggination.Skip });

        int count = await res.ReadSingleAsync<int>();
        IEnumerable<GetUserListItem> list = await res.ReadAsync<GetUserListItem>();

        return new ListQueryResponse<GetUserListItem>(list, count);
    }
}
