using Sergin.SharedKernel.Domain.Repositories;

namespace Sergin.MeterMinder.Domain.Manufacturers;
public interface IManufacturerRepository : IRepository<Manufacturer, ManufacturerId>;
