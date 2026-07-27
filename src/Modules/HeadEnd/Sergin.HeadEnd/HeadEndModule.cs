using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sergin.HeadEnd.Application;
using Sergin.HeadEnd.Devices;
using Sergin.HeadEnd.Infrastructure.Data;
using Sergin.HeadEnd.Manufacturers;
using Sergin.SharedKernel.Infrastructure.Data.EFCore;
using Sergin.SharedKernel.Modules;

namespace Sergin.HeadEnd;

public sealed class HeadEndModule : ISerginWebApiModule
{
    public string Schema => HeadEndDbContext.Schema;

    public Assembly ApplicationAssembly => HeadEndApplicationAssemblyReference.Assembly;

    public void AddServices(IServiceCollection services, IConfigurationSection configuration)
    {
        services.AddModuleDbContext<HeadEndDbContext, IHeadEndDbContext, IHeadEndUnitOfWork>(configuration, HeadEndDbContext.Schema);

        services.AddDeviceDependencies();
        services.AddManufacturerDependencies();
    }

    public Task MigrateAsync(IServiceProvider services) => services.MigrateDbContextAsync<HeadEndDbContext>();

    public void MapEndpoints(RouteGroupBuilder group) => group.MapDeviceEndpoints().MapManufacturerEndpoints();
}
