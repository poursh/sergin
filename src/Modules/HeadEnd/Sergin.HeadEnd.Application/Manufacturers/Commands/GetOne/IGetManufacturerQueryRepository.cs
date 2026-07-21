using Sergin.HeadEnd.Domain.Manufacturers;

namespace Sergin.HeadEnd.Application.Manufacturers.Commands.GetOne;

public interface IGetManufacturerQueryRepository
{
    Task<ManufacturerQueryResponse?> GetManufacturerById(ManufacturerId id, CancellationToken cancellationToken = default);
}
