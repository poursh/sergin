using Sergin.MeterMinder.Application.Manufacturers.Commands.GetList;
using Sergin.MeterMinder.Application.Manufacturers.Commands.GetOne;

namespace Sergin.MeterMinder.Application.Manufacturers;
public interface IManufacturerAllQueryRepository : IGetManufacturerListQueryRepository, IGetManufacturerQueryRepository;
