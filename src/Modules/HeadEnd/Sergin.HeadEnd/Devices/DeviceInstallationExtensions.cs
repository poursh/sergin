using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Sergin.HeadEnd.Application.Devices;
using Sergin.HeadEnd.Application.Devices.Commands.GetList;
using Sergin.HeadEnd.Application.Devices.Commands.GetOne;
using Sergin.HeadEnd.Domain.Devices;
using Sergin.HeadEnd.Infrastructure.Devices.Repositories;
using Sergin.HeadEnd.Infrastructure.Devices.Repositories.Queries;
using Sergin.HeadEnd.Presentation.Devices.Endpoints.Create;
using Sergin.HeadEnd.Presentation.Devices.Endpoints.GetList;
using Sergin.HeadEnd.Presentation.Devices.Endpoints.GetOne;

namespace Sergin.HeadEnd.Devices;

internal static class DeviceInstallationExtensions
{
    internal static IServiceCollection AddDeviceDependencies(this IServiceCollection services)
    {
        services.AddTransient<IDeviceRepository, DeviceRepository>();
        services.AddTransient<IDeviceAllQueryRepositoriy, DeviceQueryRepository>();
        services.AddTransient<IGetDeviceQueryRepository, DeviceQueryRepository>();
        services.AddTransient<IGetDeviceListQueryRepository, DeviceQueryRepository>();

        return services;
    }

    internal static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        new CreateDeviceEndpoint().MapEndpoint(routeBuilder);
        new GetDeviceEndpoint().MapEndpoint(routeBuilder);
        new GetDeviceListEndpoint().MapEndpoint(routeBuilder);

        return routeBuilder;
    }
}
