using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sergin.HeadEnd.Domain.Manufacturers;

namespace Sergin.HeadEnd.Infrastructure.Data.Manufacturers.Converters;

internal sealed class ManufacturerIdConverter : ValueConverter<ManufacturerId, Guid>
{
    private static readonly ConverterMappingHints defaultHints = new();

    public ManufacturerIdConverter() : this(null)
    {
    }

    public ManufacturerIdConverter(ConverterMappingHints? mappingHints)
        : base(
                convertToProviderExpression: x => x.Value,
                convertFromProviderExpression: x => new ManufacturerId(x),
                mappingHints: defaultHints.With(mappingHints))
    {
    }
}
