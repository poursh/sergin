using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sergin.UserAccess.Domain.Users;

namespace Sergin.UserAccess.Infrastructure.Data.Users.Converters;

internal sealed class UserInternalIdConverter : ValueConverter<UserInternalId, Guid>
{
    private static readonly ConverterMappingHints defaultHints = new();

    public UserInternalIdConverter() : this(null)
    {
    }

    public UserInternalIdConverter(ConverterMappingHints? mappingHints)
        : base(
                convertToProviderExpression: x => x.Value,
                convertFromProviderExpression: x => new UserInternalId(x),
                mappingHints: defaultHints.With(mappingHints))
    {
    }
}
