using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sergin.UserAccess.Domain.Users;
using Sergin.UserAccess.Infrastructure.Data.Users.Converters;

namespace Sergin.UserAccess.Infrastructure.Data.Users;

internal sealed class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasConversion<UserInternalIdConverter>()
            .ValueGeneratedNever();

        builder.Property(u => u.UserName)
            .HasConversion<UserNameConverter>();
    }
}
