using Microsoft.EntityFrameworkCore;
using Sergin.UserAccess.Application;
using Sergin.SharedKernel.Infrastructure.Data.EFCore;

namespace Sergin.UserAccess.Infrastructure.Data;

public interface IUserAccessDbContext : IDbContext;

internal sealed class UserAccessDbContext(DbContextOptions<UserAccessDbContext> options) : SerginDbContext(options), IUserAccessDbContext, IUserAccessUnitOfWork
{
    public const string Schema = "ua";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
