using Sergin.SharedKernel.Application.Commands.Queries;
using Sergin.SharedKernel.Application.Securities.Authorization;

namespace Sergin.HeadEnd.Application.Devices.Commands.GetOne;

[RequiredPermissions("permission.hes.devices.read")]
public sealed record GetDeviceByIdQueryCommand(Guid Id) : IQuery<DeviceQueryResponse>;
