using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sergin.HeadEnd.Application.Manufacturers.Commands.Create;
using Sergin.HeadEnd.Domain.Manufacturers;
using Sergin.SharedKernel.Presentation.WebApi.Endpoints.Results;

namespace Sergin.HeadEnd.Presentation.WebApi.Manufacturers.Endpoints.Create;

internal class CreateManufacturerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder
            .MapPost("/manufacturers", async ([FromBody] NewManufacturerModel manufacturer, ISender sender) =>
            {
                ErrorOr<CreateManufacturerCommandResponse> res = await sender.Send(
                    new CreateManufacturerCommand(
                        new ManufacturerName(manufacturer.Name),
                        manufacturer.Address is null ? null : new ManufacturerAddress(manufacturer.Address)));

                return res.ToApiResult();
            })
            .Produces<CreateManufacturerCommandResponse>();
    }
}
