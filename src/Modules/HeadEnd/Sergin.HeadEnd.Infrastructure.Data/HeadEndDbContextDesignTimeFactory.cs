using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sergin.HeadEnd.Infrastructure.Data;

internal sealed class HeadEndDbContextDesignTimeFactory() : IDesignTimeDbContextFactory<HeadEndDbContext>
{
    public HeadEndDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", false, false)
            .Build();

        string connectionString = configuration.GetConnectionString("Database");

        var optionBuilder = new DbContextOptionsBuilder<HeadEndDbContext>();

        optionBuilder
            .UseNpgsql(connectionString,
                    sqlOptions => sqlOptions
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, HeadEndDbContext.Schema))
            .UseSnakeCaseNamingConvention();

        return new HeadEndDbContext(optionBuilder.Options);
    }
}
