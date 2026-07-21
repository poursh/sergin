using Sergin.HeadEnd.Domain.Manufacturers;
using Sergin.HeadEnd.Infrastructure.Data;

namespace Sergin.HeadEnd.Infrastructure.Manufacturers.Repositories;

internal class ManufacturerRepository(IHeadEndDbContext dbContext) : IManufacturerRepository
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
