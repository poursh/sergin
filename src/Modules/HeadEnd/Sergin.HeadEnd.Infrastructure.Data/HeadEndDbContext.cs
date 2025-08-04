using Microsoft.EntityFrameworkCore;
using Sergin.HeadEnd.Application;
using Sergin.SharedKernel.Infrastructure.Data.EFCore;

namespace Sergin.HeadEnd.Infrastructure.Data;

public interface IHeadEndDbContext : IDbContext;

internal sealed class HeadEndDbContext(DbContextOptions<HeadEndDbContext> options) : SerginDbContext(options), IHeadEndDbContext, IHeadEndUnitOfWork
{
    public const string Schema = "hes";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
