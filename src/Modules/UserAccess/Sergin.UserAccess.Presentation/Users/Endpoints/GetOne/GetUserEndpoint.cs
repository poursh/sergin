using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sergin.UserAccess.Application.Users.Commands.GetOne;
using Sergin.SharedKernel.Presentation.WebApi.Endpoints.Results;

namespace Sergin.UserAccess.Presentation.Users.Endpoints.GetOne;

internal class GetUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/users/{userId:guid}", async ([FromRoute] Guid userId, ISender sender) =>
        {
            ErrorOr<UserQueryResponse> res = await sender.Send(new GetUserByIdQueryCommand(userId));

            return res.ToApiResult();
        });
    }
}
