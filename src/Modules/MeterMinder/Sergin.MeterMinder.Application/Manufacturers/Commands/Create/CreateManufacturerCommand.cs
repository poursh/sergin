using Sergin.MeterMinder.Domain.Manufacturers;
using Sergin.SharedKernel.Application.Commands;

namespace Sergin.MeterMinder.Application.Manufacturers.Commands.Create;

public sealed record CreateManufacturerCommand(ManufacturerName Name, ManufacturerAddress? Address) : ICommand<CreateManufacturerCommandResponse>;
