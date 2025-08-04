using Sergin.SharedKernel.Domain.Repositories;

namespace Sergin.HeadEnd.Domain.Devices;
public interface IDeviceRepository : IRepository<Device, DeviceIntenralId>
{
    Task<Device?> GetByDeviceId(DeviceId deviceId, CancellationToken cancellationToken = default);
}
