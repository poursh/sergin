using Sergin.MeterMinder.Domain.Manufacturers;
using Sergin.MeterMinder.Infrastructure.Data;

namespace Sergin.MeterMinder.Infrastructure.Manufacturers.Repositories;

internal class ManufacturerRepository(IMeterMinderDbContext dbContext) : IManufacturerRepository
{
    public ValueTask<Manufacturer?> GetAsync(ManufacturerId id, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<Manufacturer>().FindAsync([id, cancellationToken], cancellationToken: cancellationToken);
    }

    public void Insert(Manufacturer entity)
    {
        dbContext.Set<Manufacturer>().Add(entity);
    }

    public void Remove(Manufacturer entity)
    {
        dbContext.Set<Manufacturer>().Remove(entity);
    }
}
