using Sergin.HeadEnd.Domain.Devices;
using Sergin.HeadEnd.Domain.Manufacturers;
using Sergin.SharedKernel.Application.Commands;

namespace Sergin.HeadEnd.Application.Devices.Commands.Create;

public sealed record CreateDeviceCommand(DeviceId DeviceId, ManufacturerId ManufacturerId) : ICommand<CreateDeviceCommandResponse>;
