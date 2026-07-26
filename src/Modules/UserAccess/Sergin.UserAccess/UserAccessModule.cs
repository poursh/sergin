using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sergin.SharedKernel.Infrastructure.Data.EFCore;
using Sergin.SharedKernel.Modules;
using Sergin.UserAccess.Application;
using Sergin.UserAccess.Infrastructure.Data;
using Sergin.UserAccess.Users;

namespace Sergin.UserAccess;

public sealed class UserAccessModule : ISerginWebApiModule
{
    public string Schema => UserAccessDbContext.Schema;

    public Assembly ApplicationAssembly => UserAccessApplicationAssemblyReference.Assembly;

    public void AddServices(IServiceCollection services, IConfigurationSection configuration)
    {
        services.AddModuleDbContext<UserAccessDbContext, IUserAccessDbContext, IUserAccessUnitOfWork>(configuration, UserAccessDbContext.Schema);

        services.AddUserDependencies();
    }

    public Task MigrateAsync(IServiceProvider services) => services.MigrateDbContextAsync<UserAccessDbContext>();

    public void MapEndpoints(RouteGroupBuilder group) => group.MapUserEndpoints();
}
