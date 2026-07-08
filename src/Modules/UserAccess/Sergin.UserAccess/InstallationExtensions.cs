using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sergin.UserAccess.Application;
using Sergin.UserAccess.Users;
using Sergin.UserAccess.Infrastructure.Data;
using Sergin.SharedKernel.Infrastructure.Data.EFCore.Interceptors;

namespace Sergin.UserAccess;

public static class InstallationExtensions
{
    public static void RegisterUserAccessCommands(this MediatRServiceConfiguration configuration)
    {
        configuration.RegisterServicesFromAssembly(UserAccessApplicationAssemblyReference.Assembly);
    }
    public static IServiceCollection AddUserAccessModule(this IServiceCollection services, IConfigurationSection configuration)
    {
        services.AddDbContextAndUnitOfWork(configuration);

        services.AddUserDependencies();

        return services;
    }


    public static async Task<WebApplication> RunUserAccessModule(this WebApplication application)
    {
        if (application.Environment.IsDevelopment())
        {
            await application.ApplyMigration();
        }

        application.MapGroup("ua")
            .MapUserEndpoints();

        return application;
    }

    private static void AddDbContextAndUnitOfWork(this IServiceCollection services, IConfigurationSection configuration)
    {
        string connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<UserAccessDbContext>((sp, options) =>
            options.UseNpgsql(
                connectionString,
                pgOptions => pgOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, UserAccessDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<EventDispatcherInterceptor>()));

        services.AddScoped<IUserAccessDbContext, UserAccessDbContext>();
        services.AddScoped<IUserAccessUnitOfWork>(p => p.GetRequiredService<IUserAccessDbContext>() as UserAccessDbContext);
    }
    private static async Task ApplyMigration(this WebApplication application)
    {
        using IServiceScope scope = application.Services.CreateScope();
        using DbContext context = scope.ServiceProvider.GetRequiredService<UserAccessDbContext>();

        await context.Database.MigrateAsync();
    }
}
