using Sergin.HeadEnd.Domain.Devices;
using Sergin.SharedKernel.Application.Commands;

namespace Sergin.HeadEnd.Application.Devices.Commands.Create;

internal sealed class CreateDeviceCommandHandler(
    IHeadEndUnitOfWork unitOfWork,
    IDeviceRepository repository) : ICommandHandler<CreateDeviceCommand, CreateDeviceCommandResponse>
{
    public async Task<ErrorOr<CreateDeviceCommandResponse>> Handle(
        CreateDeviceCommand request, CancellationToken cancellationToken)
    {
        var newDevice = Device.Create(request.DeviceId);

        repository.Insert(newDevice);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateDeviceCommandResponse(newDevice.Id.Value);
    }
}
