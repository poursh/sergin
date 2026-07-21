using System.Data.Common;
using Sergin.HeadEnd.Application.Manufacturers;
using Sergin.HeadEnd.Application.Manufacturers.Commands.GetList;
using Sergin.HeadEnd.Application.Manufacturers.Commands.GetOne;
using Sergin.HeadEnd.Domain.Manufacturers;
using Sergin.SharedKernel.Application;
using Sergin.SharedKernel.Application.Commands.Queries;
using Sergin.SharedKernel.Infrastracture.Data;

namespace Sergin.HeadEnd.Infrastructure.Manufacturers.Repositories.Queries;

internal sealed class ManufacturerQueryRepository(
    IDbConnectionFactory connectionFactory) : IManufacturerAllQueryRepository
{
    public async Task<ManufacturerQueryResponse?> GetManufacturerById(
        ManufacturerId id, CancellationToken cancellationToken = default)
    {
        using DbConnection connection = await connectionFactory.CreateConnectionAsync();

        string queries =
           """
            SELECT id, name, address
            FROM hes.manufacturer
            WHERE id = @Id;
            """;

        return await connection.QuerySingleOrDefaultAsync<ManufacturerQueryResponse>(
            queries, new { Id = id.Value });
    }

    public async Task<ListQueryResponse<GetManufacturerListItem>> GetListAsync(
        ListQuery<GetManufacturerListItem> query, CancellationToken cancellationToken = default)
    {
        using DbConnection connection = await connectionFactory.CreateConnectionAsync();

        string queries =
            """
            SELECT count(*) FROM hes.manufacturer;

            SELECT id, name, address
            FROM hes.manufacturer
            LIMIT @PageSize OFFSET @Offset;
            """;

        GridReader res = await connection.QueryMultipleAsync(
            queries, new { PageSize = query.Paggination.Size.Value, Offset = query.Paggination.Skip });

        int count = await res.ReadSingleAsync<int>();
        IEnumerable<GetManufacturerListItem> list = await res.ReadAsync<GetManufacturerListItem>();

        return new ListQueryResponse<GetManufacturerListItem>(list, count);
    }
}
