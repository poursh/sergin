using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sergin.HeadEnd.Application.Devices.Commands.Create;
using Sergin.HeadEnd.Domain.Devices;
using Sergin.HeadEnd.Domain.Manufacturers;
using Sergin.SharedKernel.Presentation.WebApi.Endpoints.Results;

namespace Sergin.HeadEnd.Presentation.WebApi.Devices.Endpoints.Create;

internal class CreateDeviceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder
            .MapPost("/devices", async ([FromBody] NewDeviceModel device, ISender sender) =>
            {
                ErrorOr<CreateDeviceCommandResponse> res = await sender.Send(
                    new CreateDeviceCommand(
                        new DeviceId(device.DeviceId),
                        new ManufacturerId(device.ManufacturerId)));

                return res.ToApiResult();
            })
            .Produces<CreateDeviceCommandResponse>();
    }
}
