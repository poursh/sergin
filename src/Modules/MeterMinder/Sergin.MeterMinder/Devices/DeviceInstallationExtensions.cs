using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Sergin.MeterMinder.Application.Devices;
using Sergin.MeterMinder.Application.Devices.Commands.GetList;
using Sergin.MeterMinder.Application.Devices.Commands.GetOne;
using Sergin.MeterMinder.Domain.Devices;
using Sergin.MeterMinder.Infrastructure.Devices.Repositories;
using Sergin.MeterMinder.Infrastructure.Devices.Repositories.Queries;
using Sergin.MeterMinder.Presentation.WebApi.Devices.Endpoints.Create;
using Sergin.MeterMinder.Presentation.WebApi.Devices.Endpoints.GetList;
using Sergin.MeterMinder.Presentation.WebApi.Devices.Endpoints.GetOne;

namespace Sergin.MeterMinder.Devices;

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
