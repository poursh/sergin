using Sergin.HeadEnd.Domain.Devices;
using Sergin.SharedKernel.Application.Commands;

namespace Sergin.HeadEnd.Application.Devices.Commands.Create;

public sealed record CreateDeviceCommand(DeviceId DeviceId) : ICommand<CreateDeviceCommandResponse>;
