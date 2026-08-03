using Microsoft.EntityFrameworkCore;
using Sergin.MeterMinder.Domain.Devices;
using Sergin.MeterMinder.Infrastructure.Data;

namespace Sergin.MeterMinder.Infrastructure.Devices.Repositories;

internal class DeviceRepository(IMeterMinderDbContext dbContext) : IDeviceRepository
{
    public ValueTask<Device?> GetAsync(DeviceIntenralId id, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<Device>().FindAsync([id, cancellationToken], cancellationToken: cancellationToken);
    }

    public Task<Device?> GetByDeviceId(DeviceId deviceId, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<Device>().SingleOrDefaultAsync(d => d.DeviceId == deviceId, cancellationToken);
    }

    public void Insert(Device entity)
    {
        dbContext.Set<Device>().Add(entity);
    }

    public void Remove(Device entity)
    {
        dbContext.Set<Device>().Remove(entity);
    }
}
