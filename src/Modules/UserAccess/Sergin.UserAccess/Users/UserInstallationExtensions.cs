using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Sergin.UserAccess.Application.Users;
using Sergin.UserAccess.Application.Users.Commands.GetList;
using Sergin.UserAccess.Application.Users.Commands.GetOne;
using Sergin.UserAccess.Domain.Users;
using Sergin.UserAccess.Infrastructure.Users.Repositories;
using Sergin.UserAccess.Infrastructure.Users.Repositories.Queries;
using Sergin.UserAccess.Presentation.WebApi.Users.Endpoints.Create;
using Sergin.UserAccess.Presentation.WebApi.Users.Endpoints.DeactivateUser;
using Sergin.UserAccess.Presentation.WebApi.Users.Endpoints.GetList;
using Sergin.UserAccess.Presentation.WebApi.Users.Endpoints.GetOne;

namespace Sergin.UserAccess.Users;

internal static class UserInstallationExtensions
{
    internal static IServiceCollection AddUserDependencies(this IServiceCollection services)
    {
        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IUserAllQueryRepository, UserQueryRepository>();
        services.AddTransient<IGetUserQueryRepository, UserQueryRepository>();
        services.AddTransient<IGetUserListQueryRepository, UserQueryRepository>();

        return services;
    }

    internal static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        new CreateUserEndpoint().MapEndpoint(routeBuilder);
        new DeactivateUserEndpoint().MapEndpoint(routeBuilder);
        new GetUserEndpoint().MapEndpoint(routeBuilder);
        new GetUserListEndpoint().MapEndpoint(routeBuilder);

        return routeBuilder;
    }
}
