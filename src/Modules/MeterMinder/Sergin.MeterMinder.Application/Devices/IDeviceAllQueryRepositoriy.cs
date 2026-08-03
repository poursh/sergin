using Sergin.MeterMinder.Application.Devices.Commands.GetList;
using Sergin.MeterMinder.Application.Devices.Commands.GetOne;

namespace Sergin.MeterMinder.Application.Devices;
public interface IDeviceAllQueryRepositoriy : IGetDeviceListQueryRepository, IGetDeviceQueryRepository;
