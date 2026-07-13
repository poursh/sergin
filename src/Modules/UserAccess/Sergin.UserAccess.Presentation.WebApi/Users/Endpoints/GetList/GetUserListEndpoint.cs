using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sergin.UserAccess.Application.Users.Commands.GetList;
using Sergin.SharedKernel.Application;
using Sergin.SharedKernel.Presentation.WebApi.Endpoints.Results;

namespace Sergin.UserAccess.Presentation.WebApi.Users.Endpoints.GetList;
internal class GetUserListEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder
            .MapGet("/users", async ([AsParameters] ListQueryRequestModel request, ISender sender) =>
            {
                ErrorOr<ListQueryResponse<GetUserListItem>> res = await sender.Send(
                    request.ToListQuery<GetUserListItem>());

                return res.ToApiResult();
            })
            .Produces<ListQueryResponse<GetUserListItem>>();
    }
}
