using Sergin.MeterMinder.Domain.Manufacturers;

namespace Sergin.MeterMinder.Application.Manufacturers.Commands.GetOne;

public interface IGetManufacturerQueryRepository
{
    Task<ManufacturerQueryResponse?> GetManufacturerById(ManufacturerId id, CancellationToken cancellationToken = default);
}
