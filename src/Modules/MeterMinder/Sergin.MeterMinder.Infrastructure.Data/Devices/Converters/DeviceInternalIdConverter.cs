using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sergin.MeterMinder.Domain.Devices;

namespace Sergin.MeterMinder.Infrastructure.Data.Devices.Converters;

internal sealed class DeviceInternalIdConverter : ValueConverter<DeviceIntenralId, Guid>
{
    private static readonly ConverterMappingHints defaultHints = new();

    public DeviceInternalIdConverter() : this(null)
    {
    }

    public DeviceInternalIdConverter(ConverterMappingHints? mappingHints)
        : base(
                convertToProviderExpression: x => x.Value,
                convertFromProviderExpression: x => new DeviceIntenralId(x),
                mappingHints: defaultHints.With(mappingHints))
    {
    }
}
