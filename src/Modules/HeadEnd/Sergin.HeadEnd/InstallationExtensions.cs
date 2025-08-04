using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sergin.HeadEnd.Application;
using Sergin.HeadEnd.Devices;
using Sergin.HeadEnd.Infrastructure.Data;
using Sergin.SharedKernel.Infrastructure.Data.EFCore.Interceptors;

namespace Sergin.HeadEnd;

public static class InstallationExtensions
{
    public static void RegisterHeadEndCommands(this MediatRServiceConfiguration configuration)
    {
        configuration.RegisterServicesFromAssembly(HeadEndApplicationAssemblyReference.Assembly);
    }
    public static IServiceCollection AddHeadEndModule(this IServiceCollection services, IConfigurationSection configuration)
    {
        services.AddDbContextAndUnitOfWork(configuration);

        services.AddDeviceDependencies();

        return services;
    }


    public static async Task<WebApplication> RunHeadEndModule(this WebApplication application)
    {
        if (application.Environment.IsDevelopment())
        {
            await application.ApplyMigration();
        }

        application.MapGroup("hes")
            .MapDeviceEndpoints();

        return application;
    }

    private static IServiceCollection AddDbContextAndUnitOfWork(this IServiceCollection services, IConfigurationSection configuration)
    {
        string connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<HeadEndDbContext>((sp, options) =>
            options.UseNpgsql(
                connectionString,
                pgOptions => pgOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, HeadEndDbContext.Schema))
            .UseSnakeCaseNamingConvention()            
            .AddInterceptors(sp.GetRequiredService<EventDispatcherInterceptor>()));

        services.AddScoped<IHeadEndDbContext, HeadEndDbContext>();
        services.AddScoped<IHeadEndUnitOfWork>(p => p.GetRequiredService<IHeadEndDbContext>() as HeadEndDbContext);

        return services;
    }
    private static async Task ApplyMigration(this WebApplication application)
    {
        using IServiceScope scope = application.Services.CreateScope();
        using DbContext context = scope.ServiceProvider.GetRequiredService<HeadEndDbContext>();

        await context.Database.MigrateAsync();
    }
}
