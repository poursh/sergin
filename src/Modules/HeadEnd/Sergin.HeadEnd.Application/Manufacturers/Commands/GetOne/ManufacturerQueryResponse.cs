namespace Sergin.HeadEnd.Application.Manufacturers.Commands.GetOne;

public sealed record ManufacturerQueryResponse(Guid Id, string Name, string? Address);
