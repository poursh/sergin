# UserAccess module

Schema `ua`.

See the root `.claude/CLAUDE.md` for cross-module conventions (layering, CQRS split, permissions, etc.) — this file only covers what's specific to the `Users` aggregate.

## `Users` aggregate

`Sergin.UserAccess.Domain/Users/User.cs` — `AggregateRoot<UserInternalId>`, private ctor + `static Create(UserName)` factory. `IsActive` defaults to `true` on creation and is flipped by the `Deactivate()` method.

Implemented feature slices (`Users/Commands/<Feature>/` in Application, mirrored in Infrastructure/Presentation):

| Feature | Kind | Route | Permission |
|---|---|---|---|
| `Create` | command | `POST /ua/users` | none |
| `DeactivateUser` | command | `POST /ua/users/{userId:guid}/deactivate` | none |
| `GetOne` | query | `GET /ua/users/{userId:guid}` | `permission.ua.users.read` |
| `GetList` | query | `GET /ua/users` (`[AsParameters] ListQueryRequestModel`) | none |

Only `GetOne` carries a `[RequiredPermissions]` attribute — that's the current state of the module, not a rule that only reads need it. Add the attribute to new slices that should be protected; don't remove it from `GetOne` to "match" the others.

## Repositories

- `IUserRepository` (`Domain/Users/`) — plain `IRepository<User, UserInternalId>`, no custom methods beyond the generic CRUD, implemented by `UserRepository` (EF Core) in Infrastructure.
- Query repositories are split one-interface-per-feature (`IGetUserQueryRepository`, `IGetUserListQueryRepository`, plus a module-wide `IUserAllQueryRepository`), all implemented by a single `UserQueryRepository` class using `IDbConnectionFactory` + raw SQL. Follow this split (new interface per query feature, one class implementing all of them) rather than one fat repository interface.
