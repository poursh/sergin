# HeadEnd module

Schema `hes`. 

See the root `.claude/CLAUDE.md` for cross-module conventions (layering, CQRS split, permissions, etc.) — this file only covers what's specific to the `Devices`/`DeviceModels` aggregates.

## `Devices` aggregate

`Sergin.HeadEnd.Domain/Devices/Device.cs` — `AggregateRoot<DeviceIntenralId>` (note the misspelling — it's the real type name, match it). `DeviceId` is the business-facing string key; `DeviceIntenralId` is the internal `Guid` PK.

**`DeviceModel` is an unfinished, dangling piece**: `Sergin.HeadEnd.Domain/DeviceModels/DeviceModel.cs` defines a `DeviceModel` aggregate, and `Device.cs` has a commented-out `ModelId`/`Model` relationship and a commented-out `Create(DeviceModelInternalId)` factory overload. Neither is wired into anything — no repository, no Application slice, no endpoint, no EF configuration. Don't build a new feature on top of this relationship without checking with the user first; if you need device-model data, treat `DeviceModel` as a bare aggregate stub, not an established pattern.

Implemented feature slices (`Devices/Commands/<Feature>/` in Application, mirrored in Infrastructure/Presentation):

| Feature | Kind | Route | Permission |
|---|---|---|---|
| `Create` | command | `POST /hes/devices` | none |
| `GetOne` | query | `GET /hes/devices/{deviceId:guid}` | `permission.hes.devices.read` |
| `GetList` | query | `GET /hes/devices` (`[AsParameters] ListQueryRequestModel`) | none |

## Repositories

- `IDeviceRepository` (`Domain/Devices/`) extends the generic `IRepository<Device, DeviceIntenralId>` with one extra method, `GetByDeviceId(DeviceId)` — a precedent for adding aggregate-specific lookups to the repository interface when the generic CRUD isn't enough, rather than reaching into EF from the Application layer.
- Query repositories follow the same one-interface-per-feature split as UserAccess (`IGetDeviceQueryRepository`, `IGetDeviceListQueryRepository`, `IDeviceAllQueryRepositoriy` — note the existing typo in that last name, match it), all implemented by a single `DeviceQueryRepository` class.
