using Sergin.HeadEnd.Application.Devices.Commands.GetList;
using Sergin.HeadEnd.Application.Devices.Commands.GetOne;

namespace Sergin.HeadEnd.Application.Devices;
public interface IDeviceAllQueryRepositoriy : IGetDeviceListQueryRepository, IGetDeviceQueryRepository;
