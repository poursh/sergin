using Sergin.SharedKernel.Application.Commands.Queries;
using Sergin.SharedKernel.Application.Securities.Authorization;

namespace Sergin.MeterMinder.Application.Manufacturers.Commands.GetOne;

[RequiredPermissions("permission.mm.manufacturers.read")]
public sealed record GetManufacturerByIdQueryCommand(Guid Id) : IQuery<ManufacturerQueryResponse>;
