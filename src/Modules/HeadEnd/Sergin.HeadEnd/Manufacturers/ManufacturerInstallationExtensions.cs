using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Sergin.HeadEnd.Application.Manufacturers;
using Sergin.HeadEnd.Application.Manufacturers.Commands.GetList;
using Sergin.HeadEnd.Application.Manufacturers.Commands.GetOne;
using Sergin.HeadEnd.Domain.Manufacturers;
using Sergin.HeadEnd.Infrastructure.Manufacturers.Repositories;
using Sergin.HeadEnd.Infrastructure.Manufacturers.Repositories.Queries;
using Sergin.HeadEnd.Presentation.WebApi.Manufacturers.Endpoints.Create;
using Sergin.HeadEnd.Presentation.WebApi.Manufacturers.Endpoints.GetList;
using Sergin.HeadEnd.Presentation.WebApi.Manufacturers.Endpoints.GetOne;

namespace Sergin.HeadEnd.Manufacturers;

internal static class ManufacturerInstallationExtensions
{
    internal static IServiceCollection AddManufacturerDependencies(this IServiceCollection services)
    {
        services.AddTransient<IManufacturerRepository, ManufacturerRepository>();
        services.AddTransient<IManufacturerAllQueryRepository, ManufacturerQueryRepository>();
        services.AddTransient<IGetManufacturerQueryRepository, ManufacturerQueryRepository>();
        services.AddTransient<IGetManufacturerListQueryRepository, ManufacturerQueryRepository>();

        return services;
    }

    internal static IEndpointRouteBuilder MapManufacturerEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        new CreateManufacturerEndpoint().MapEndpoint(routeBuilder);
        new GetManufacturerEndpoint().MapEndpoint(routeBuilder);
        new GetManufacturerListEndpoint().MapEndpoint(routeBuilder);

        return routeBuilder;
    }
}
