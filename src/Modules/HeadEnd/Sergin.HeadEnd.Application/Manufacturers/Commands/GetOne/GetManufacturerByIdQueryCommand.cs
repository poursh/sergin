using Sergin.SharedKernel.Application.Commands.Queries;
using Sergin.SharedKernel.Application.Securities.Authorization;

namespace Sergin.HeadEnd.Application.Manufacturers.Commands.GetOne;

[RequiredPermissions("permission.hes.manufacturers.read")]
public sealed record GetManufacturerByIdQueryCommand(Guid Id) : IQuery<ManufacturerQueryResponse>;
