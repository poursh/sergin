using Microsoft.EntityFrameworkCore;
using Sergin.MeterMinder.Application;
using Sergin.SharedKernel.Infrastructure.Data.EFCore;

namespace Sergin.MeterMinder.Infrastructure.Data;

public interface IMeterMinderDbContext : IDbContext;

internal sealed class MeterMinderDbContext(DbContextOptions<MeterMinderDbContext> options) : SerginDbContext(options), IMeterMinderDbContext, IMeterMinderUnitOfWork
{
    public const string Schema = "mm";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
