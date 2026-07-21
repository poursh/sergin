using Sergin.HeadEnd.Domain.Manufacturers;
using Sergin.SharedKernel.Application.Commands;

namespace Sergin.HeadEnd.Application.Manufacturers.Commands.Create;

public sealed record CreateManufacturerCommand(ManufacturerName Name, ManufacturerAddress? Address) : ICommand<CreateManufacturerCommandResponse>;
