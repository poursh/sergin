using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Sergin.IntegrationTests;

public sealed class SerginApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ConnectionStringEnvVariable = "Sergin__ConnectionStrings__Database";

    private readonly PostgreSqlContainer dbContainer = new PostgreSqlBuilder("postgres:17").Build();

    public async Task InitializeAsync()
    {
        await dbContainer.StartAsync();

        // Set before the host builds (ConfigureWebHost config overrides run too late for this).
        Environment.SetEnvironmentVariable(ConnectionStringEnvVariable, dbContainer.GetConnectionString());
    }

    public new async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable(ConnectionStringEnvVariable, null);
        await dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}
