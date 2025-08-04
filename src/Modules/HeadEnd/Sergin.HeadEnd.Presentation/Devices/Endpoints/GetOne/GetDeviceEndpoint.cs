﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sergin.HeadEnd.Application.Devices.Commands.GetOne;
using Sergin.SharedKernel.Presentation.WebApi.Endpoints.Results;

namespace Sergin.HeadEnd.Presentation.Devices.Endpoints.GetOne;

internal class GetDeviceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/devices/{deviceId:guid}", async ([FromRoute] Guid deviceId, ISender sender) =>
        {
            ErrorOr<DeviceQueryResponse> res = await sender.Send(new GetDeviceByIdQueryCommand(deviceId));

            return res.ToApiResult();
        });
    }
}
