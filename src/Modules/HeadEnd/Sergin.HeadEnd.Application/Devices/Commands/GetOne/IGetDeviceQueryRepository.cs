using Sergin.HeadEnd.Domain.Devices;

namespace Sergin.HeadEnd.Application.Devices.Commands.GetOne;

public interface IGetDeviceQueryRepository
{
    Task<DeviceQueryResponse?> GetDeviceById(DeviceIntenralId Id, CancellationToken cancellationToken = default);
}
