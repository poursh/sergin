using Sergin.MeterMinder.Domain.Devices;

namespace Sergin.MeterMinder.Application.Devices.Commands.GetOne;

public interface IGetDeviceQueryRepository
{
    Task<DeviceQueryResponse?> GetDeviceById(DeviceIntenralId Id, CancellationToken cancellationToken = default);
}
