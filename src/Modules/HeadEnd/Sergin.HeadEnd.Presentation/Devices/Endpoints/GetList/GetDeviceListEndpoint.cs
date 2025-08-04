﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sergin.HeadEnd.Application.Devices.Commands.GetList;
using Sergin.SharedKernel.Application;
using Sergin.SharedKernel.Presentation.WebApi.Endpoints.Results;

namespace Sergin.HeadEnd.Presentation.Devices.Endpoints.GetList;
internal class GetDeviceListEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder
            .MapGet("/devices", async ([AsParameters]ListQueryRequestModel request, ISender sender) =>
            {
                ErrorOr<ListQueryResponse<GetDeviceListItem>> res = await sender.Send(
                    request.ToListQuery<GetDeviceListItem>());

                return res.ToApiResult();
            })
            .Produces<ListQueryResponse<GetDeviceListItem>>();
    }
}
