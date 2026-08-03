using Sergin.MeterMinder.Domain.Devices;
using Sergin.MeterMinder.Domain.Manufacturers;
using Sergin.SharedKernel.Application.Commands;

namespace Sergin.MeterMinder.Application.Devices.Commands.Create;

public sealed record CreateDeviceCommand(DeviceId DeviceId, ManufacturerId ManufacturerId) : ICommand<CreateDeviceCommandResponse>;
