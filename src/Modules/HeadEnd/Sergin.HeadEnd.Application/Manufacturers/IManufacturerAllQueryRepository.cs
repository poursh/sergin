using Sergin.HeadEnd.Application.Manufacturers.Commands.GetList;
using Sergin.HeadEnd.Application.Manufacturers.Commands.GetOne;

namespace Sergin.HeadEnd.Application.Manufacturers;
public interface IManufacturerAllQueryRepository : IGetManufacturerListQueryRepository, IGetManufacturerQueryRepository;
