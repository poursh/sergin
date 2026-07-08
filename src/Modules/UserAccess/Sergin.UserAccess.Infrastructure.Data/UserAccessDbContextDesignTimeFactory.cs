using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sergin.UserAccess.Infrastructure.Data;

internal sealed class UserAccessDbContextDesignTimeFactory() : IDesignTimeDbContextFactory<UserAccessDbContext>
{
    public UserAccessDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", false, false)
            .Build();

        string connectionString = configuration.GetConnectionString("Database");

        var optionBuilder = new DbContextOptionsBuilder<UserAccessDbContext>();

        optionBuilder
            .UseNpgsql(connectionString,
                    sqlOptions => sqlOptions
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, UserAccessDbContext.Schema))
            .UseSnakeCaseNamingConvention();

        return new UserAccessDbContext(optionBuilder.Options);
    }
}
