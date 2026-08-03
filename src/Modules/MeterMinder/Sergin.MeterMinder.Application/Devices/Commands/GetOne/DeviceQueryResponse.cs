namespace Sergin.MeterMinder.Application.Devices.Commands.GetOne;

public sealed record DeviceQueryResponse(Guid Id, string DeviceId, Guid ManufacturerId);
