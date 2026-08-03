using Sergin.MeterMinder.Domain.Manufacturers;
using Sergin.SharedKernel.Application.Commands;

namespace Sergin.MeterMinder.Application.Manufacturers.Commands.Create;

internal sealed class CreateManufacturerCommandHandler(
    IMeterMinderUnitOfWork unitOfWork,
    IManufacturerRepository repository) : ICommandHandler<CreateManufacturerCommand, CreateManufacturerCommandResponse>
{
    public async Task<ErrorOr<CreateManufacturerCommandResponse>> Handle(
        CreateManufacturerCommand request, CancellationToken cancellationToken)
    {
        var newManufacturer = Manufacturer.Create(request.Name, request.Address);

        repository.Insert(newManufacturer);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateManufacturerCommandResponse(newManufacturer.Id.Value);
    }
}
