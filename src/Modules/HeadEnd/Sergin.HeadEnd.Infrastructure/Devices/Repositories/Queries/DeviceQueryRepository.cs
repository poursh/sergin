﻿using System.Data.Common;
using Sergin.HeadEnd.Application.Devices;
using Sergin.HeadEnd.Application.Devices.Commands.GetList;
using Sergin.SharedKernel.Application;
using Sergin.SharedKernel.Application.Commands.Queries;
using Sergin.SharedKernel.Infrastracture.Data;
using Sergin.HeadEnd.Application.Devices.Commands.GetOne;
using Sergin.HeadEnd.Domain.Devices;

namespace Sergin.HeadEnd.Infrastructure.Devices.Repositories.Queries;

internal sealed class DeviceQueryRepository(
    IDbConnectionFactory connectionFactory) : IDeviceAllQueryRepositoriy
{
    public async Task<DeviceQueryResponse?> GetDeviceById(
        DeviceIntenralId Id, CancellationToken cancellationToken = default)
    {
        using DbConnection connection = await connectionFactory.CreateConnectionAsync();

        string queries =
           """
            SELECT id, device_id AS deviceId 
            FROM hes.device
            WHERE id = @Id;
            """;

        return await connection.QuerySingleOrDefaultAsync<DeviceQueryResponse>(
            queries, new { Id = Id.Value });
    }

    public async Task<ListQueryResponse<GetDeviceListItem>> GetListAsync(
        ListQuery<GetDeviceListItem> query, CancellationToken cancellationToken = default)
    {
        using DbConnection connection = await connectionFactory.CreateConnectionAsync();

        string queries =
            """
            SELECT count(*) FROM hes.device;

            SELECT id, device_id AS deviceId 
            FROM hes.device
            LIMIT @PageSize OFFSET @Offset;
            """;

        GridReader res = await connection.QueryMultipleAsync(
            queries, new { PageSize = query.Paggination.Size.Value, Offset = query.Paggination.Skip });

        int count = await res.ReadSingleAsync<int>();
        IEnumerable<GetDeviceListItem> list = await res.ReadAsync<GetDeviceListItem>();

        return new ListQueryResponse<GetDeviceListItem>(list, count);
    }
}
