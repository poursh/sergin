using Sergin.SharedKernel.Application.Commands.Queries;

namespace Sergin.HeadEnd.Application.Manufacturers.Commands.GetList;
public interface IGetManufacturerListQueryRepository
{
    Task<ListQueryResponse<GetManufacturerListItem>> GetListAsync(ListQuery<GetManufacturerListItem> query, CancellationToken cancellationToken = default);
}
