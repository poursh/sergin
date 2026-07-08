using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sergin.UserAccess.Domain.Users;

namespace Sergin.UserAccess.Infrastructure.Data.Users.Converters;

internal sealed class UserNameConverter : ValueConverter<UserName, string>
{
    private static readonly ConverterMappingHints defaultHints = new();

    public UserNameConverter() : this(null)
    {
    }

    public UserNameConverter(ConverterMappingHints? mappingHints)
        : base(
                convertToProviderExpression: x => x.Value,
                convertFromProviderExpression: x => new UserName(x),
                mappingHints: defaultHints.With(mappingHints))
    {
    }
}
