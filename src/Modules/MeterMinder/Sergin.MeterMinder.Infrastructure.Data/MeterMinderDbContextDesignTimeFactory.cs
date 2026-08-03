using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sergin.MeterMinder.Infrastructure.Data;

internal sealed class MeterMinderDbContextDesignTimeFactory() : IDesignTimeDbContextFactory<MeterMinderDbContext>
{
    public MeterMinderDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", false, false)
            .Build();

        string connectionString = configuration.GetSection("Sergin").GetConnectionString("Database");

        var optionBuilder = new DbContextOptionsBuilder<MeterMinderDbContext>();

        optionBuilder
            .UseNpgsql(connectionString,
                    sqlOptions => sqlOptions
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, MeterMinderDbContext.Schema))
            .UseSnakeCaseNamingConvention();

        return new MeterMinderDbContext(optionBuilder.Options);
    }
}
