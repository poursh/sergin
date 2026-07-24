# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Sergin is a .NET 10 **modular monolith** for a HES (Head-End System) platform, utility smart meters (electricity/gas/water head-end system), built with DDD + Clean Architecture and per-feature vertical slices. It uses .NET Aspire for local orchestration and PostgreSQL for storage. There are currently two modules: **HeadEnd** and **UserAccess**.

## Commands

Run all commands from the repo root. The solution uses the modern XML format (`Sergin.slnx`); pass it explicitly or run from the repo root so the CLI resolves it automatically. Requires the .NET 10 SDK / VS 17.13+ / Rider.

```bash
# Build (warnings are errors — see below)
dotnet build Sergin.slnx

# Run the API directly (all-in-one host, Development profile applies EF migrations on startup)
dotnet run --project src/Hosts/Sergin.Hosts.WebApi.All      # http://localhost:5000, Scalar UI at /scalar/v1

# Run everything in Docker (API + postgres:17 + Aspire dashboard)
docker compose -f docker-compose/docker-compose.yml up --build

# Run the integration test suite (needs Docker — spins up a real postgres:17 via Testcontainers)
dotnet test tests/Sergin.IntegrationTests/Sergin.IntegrationTests.csproj
```

`tests/Sergin.IntegrationTests` is the only test project so far — xUnit + `Testcontainers.PostgreSql` +
`Microsoft.AspNetCore.Mvc.Testing`, exercising the real `Sergin.Hosts.WebApi.All` host end-to-end
(HTTP → command/query handler → EF write or raw-SQL read) against a disposable container rather than
mocks. There are no unit test projects yet.

### EF Core migrations

Each module owns its own `DbContext` and migrations, so `--project` must point at that module's `Infrastructure.Data` project. `HeadEndDbContext` and `UserAccessDbContext` each have an `IDesignTimeDbContextFactory` that reads `appsettings.Development.json` for the connection string:

```bash
dotnet ef migrations add <Name> \
  --project src/Modules/HeadEnd/Sergin.HeadEnd.Infrastructure.Data \
  --startup-project src/Hosts/Sergin.Hosts.WebApi.All

dotnet ef migrations add <Name> \
  --project src/Modules/UserAccess/Sergin.UserAccess.Infrastructure.Data \
  --startup-project src/Hosts/Sergin.Hosts.WebApi.All
```

Migrations are applied automatically at startup **only in the Development environment** (`Run<Module>Module` → `ApplyMigration`, called for every module from `Sergin.Hosts.WebApi.All/Program.cs`).

## Git conventions

- **Commit authorship**: Never add a `Co-Authored-By: Claude` trailer or otherwise attribute commits to Claude/the assistant. Commit under the user's configured git identity only.

## Critical build constraint

`Directory.Build.props` sets `TreatWarningsAsErrors=true`, `AnalysisMode=All`, and enables **SonarAnalyzer.CSharp** + `EnforceCodeStyleInBuild`. Any analyzer warning, style violation, or nullable warning **fails the build**. Nullable and implicit usings are enabled solution-wide. Write code that passes analysis cleanly the first time.

## Architecture

### Host / module composition

- **`Sergin.Hosts.WebApi.All`** — the actual runnable Web API ("all-in-one" host). Its `Program.cs` is the composition root: registers MediatR (calling each module's `Register<Module>Commands()`) + pipeline behaviors, EF interceptors, `IDbConnectionFactory`, user context, localizer, and calls `builder.Services.Add<Module>Module(...)` / `app.Run<Module>Module()` once per module.
- **`Sergin.Hosts.Shared`** — Aspire service defaults (OpenTelemetry, health checks).
- **Modules** live under `src/Modules/<ModuleName>/`: currently **`HeadEnd`** (schema `hes`) and **`UserAccess`** (schema `ua`). A module is wired into the host through its **`InstallationExtensions`** (in the `Sergin.<Module>` composition project, no suffix): `Add<Module>Module` registers DI + DbContext; `Run<Module>Module` maps endpoints under a route group and applies migrations. Each module has its own `CLAUDE.md` (`src/Modules/<Module>/CLAUDE.md`) covering aggregate-specific details (implemented feature slices, quirks, unfinished pieces) that don't belong here.

### Per-module project layering

A module is split into projects that enforce Clean Architecture dependency direction. **`src/Modules/UserAccess/**/Users/**` is the canonical reference implementation** — it's the most complete and current slice; when in doubt about the "right" shape for a new feature, read the matching file there before writing the new one.

- **`.Domain`** — aggregates/entities, strongly-typed IDs, repository interfaces. Depends only on `SharedKernel.Domain`. Aggregates are built via a private/parameterless constructor + a `static Create(...)` factory method (e.g. `User.Create(UserName)`, `Device.Create(...)`) — no public setters; mutate via named methods on the aggregate (e.g. `User.Deactivate()`).
  - ID generation always uses `Guid.CreateVersion7()`, never `Guid.NewGuid()` — e.g. `new UserInternalId(Guid.CreateVersion7())`; `RowVersion.Create()` follows the same call.
  - `Create(...)` returns via **object-initializer syntax** against the private parameterless constructor (`new User { Id = ..., UserName = userName, IsActive = true }`), not a parameterized constructor call — match this shape for new aggregates.
  - Strongly-typed IDs/value objects are declared as trailing `sealed record`s in the **same file** as their owning aggregate (e.g. `UserInternalId` and `UserName` both live in `User.cs`), not split into separate files.
- **`.Application`** — MediatR commands/queries + handlers, `IUnitOfWork`, query repository interfaces. Feature folders hold the full slice under `<Aggregate>/Commands/<Feature>/...` — **queries live under `Commands/` too**, not a separate `Queries/` folder; don't invent one.
- **`.Infrastructure`** — write-side repositories (EF Core) and read-side query repositories (raw SQL via `IDbConnectionFactory`).
  - Generic PK lookup uses the array-args overload: `dbContext.Set<T>().FindAsync([id, cancellationToken], cancellationToken: cancellationToken)`, not `FindAsync(id, cancellationToken)`.
  - Aggregate-specific lookups (`GetByUserName`, `GetByDeviceId`) use `SingleOrDefaultAsync(x => x.Field == value, cancellationToken)` and are added directly to the repository interface (`IUserRepository`, `IDeviceRepository`) — this is the precedent for adding a lookup beyond generic CRUD, rather than reaching into EF from the Application layer.
- **`.Infrastructure.Data`** — the module's `DbContext`, `IEntityTypeConfiguration`s, value converters, and migrations.
  - Value converter template for a wrapped value object — copy this skeleton rather than re-deriving it:
    ```csharp
    internal sealed class FooConverter : ValueConverter<Foo, TPrimitive>
    {
        private static readonly ConverterMappingHints defaultHints = new();
        public FooConverter() : this(null) { }
        public FooConverter(ConverterMappingHints? mappingHints)
            : base(x => x.Value, x => new Foo(x), defaultHints.With(mappingHints)) { }
    }
    ```
    For a **nullable** wrapped value object, both type params and both conversion expressions get a null ternary instead (`ValueConverter<Foo?, TPrimitive?>`, `x => x == null ? null : x.Value` / `x => x == null ? null : new Foo(x)`) — see `ManufacturerAddressConverter` as the reference example.
- **`.Presentation.WebApi`** — minimal-API endpoints implementing `IEndpoint`.
- **`Sergin.<Module>`** (no suffix) — the module's composition root that references all the above and exposes the installation extensions.

### Adding a new feature

Use the **`/add-feature`** skill (`.claude/skills/add-feature/SKILL.md`) to scaffold a new CQRS vertical slice (command or query) — it encodes the full file-by-file layout (Application handler, Infrastructure repository wiring, Presentation endpoint, DI/route registration) following the UserAccess/Users reference pattern. Don't hand-roll the layout from memory; invoke the skill or read it for the authoritative shape.

### CQRS split

- **Writes**: endpoint → MediatR `ICommand` → `ICommandHandler` → domain `AggregateRoot` factory/behavior method → `IRepository` (EF Core) → `IUnitOfWork.SaveChangesAsync`. Each module has its own unit of work (e.g. `IHeadEndUnitOfWork`, `IUserAccessUnitOfWork`), implemented by its `DbContext`.
- **Reads**: query handlers use dedicated query-repository interfaces (`I<Feature>QueryRepository`) backed by **raw SQL through `IDbConnectionFactory`** (Dapper-style `QuerySingleOrDefaultAsync` / `QueryMultipleAsync`), bypassing EF entirely for read models. A query handler maps a `null` result to `Error.NotFound()`.
  - Each query method opens its own `using DbConnection connection = await connectionFactory.CreateConnectionAsync();` — connections aren't shared or injected, one per call.
  - SQL is a raw `"""..."""` string literal; snake_case columns are aliased to match the response record's exact property casing so Dapper's binder matches (`SELECT user_name AS userName FROM ua.users WHERE id = @Id;`).
  - List queries batch **two** statements through one `QueryMultipleAsync` call — a `SELECT count(*) ...;` followed by the paged `SELECT ... LIMIT @PageSize OFFSET @Offset;` — then read them off the same `GridReader` (`ReadSingleAsync<int>()` then `ReadAsync<TItem>()`), wrapped as `new ListQueryResponse<TItem>(list, count)`. Both `UserQueryRepository` and `DeviceQueryRepository` use this exact shape.
  - The not-found idiom is **bare `Error.NotFound()`** with no custom code/description. Since `ApiProblemResults` localizes on `error.Code`, every not-found response across the API currently renders identical generic text regardless of aggregate — don't invent a per-feature `Error.NotFound(code, description)` without first checking the localization resources support it.

### CQRS structural gotchas

- **List-query features have no dedicated request record.** `Get<Aggregate>ListQueryCommandHandler` implements `IListQueryHandler<TItem>` directly against the shared generic `ListQuery<TItem>` (built by `ListQueryRequestModel.ToListQuery<TItem>()` in the endpoint) — there is no `Get<Aggregate>ListQueryCommand` type to attribute. This is *why* `[RequiredPermissions]` can't be applied to any `GetList` slice today — a structural gap in the shared generic type, not an inconsistently-applied convention. If a list feature needs authorization, that requires introducing a feature-specific list-query type first; there's no existing precedent for that shape, so flag it to the user rather than guessing.
- **`.Produces<TResponse>()` is called on Create/GetList endpoints but omitted on GetOne endpoints**, consistently in both modules. Match whichever family you're extending rather than "completing" the other.
- **Endpoint route strings never include the schema segment** (`/users`, not `/ua/users`) — the schema prefix is added exactly once via `application.MapGroup("<schema>")` in the module's `Run<Module>Module`.
- **No FK-existence check on write.** `CreateDeviceCommandHandler` inserts a `Device` referencing `ManufacturerId` without checking the manufacturer exists — a bad ID surfaces as a raw Postgres FK-violation exception, not an `ErrorOr` result. This is the current state of the only cross-aggregate FK in the codebase, not an established pattern to replicate — no existing slice shows how to convert this into a friendly `ErrorOr` error.

### Cross-cutting conventions

- **Results**: handlers return `ErrorOr<T>` (the `ErrorOr` library, global-imported). Endpoints call `.ToApiResult()` to convert to an `IResult`/ProblemDetails.
- **MediatR pipeline behaviors** (registered in `Program.cs`, order matters):
  1. `PermissionCheckPipelineBehavior` — enforces `[RequiredPermissionsAttribute]` on any `IBaseCommand` (covers both commands and queries) against `IUserContext`.
  2. `ValidationPipelineBehavior` — runs an optional FluentValidation `IValidator<TRequest>` if one is registered.
- **Permissions**: apply `[RequiredPermissions("permission.<schema>.<resource>.<action>")]` to a command/query record when it needs authorization, e.g. `"permission.ua.users.read"`, `"permission.hes.devices.read"`. This is opt-in per slice, not universally applied today — most commands have no attribute yet, so don't assume its absence on an existing handler is an oversight to fix incidentally.
- **Validation**: FluentValidation is wired but optional — no `AbstractValidator<T>` exists in the codebase yet. Add one alongside a command/query only when the feature actually needs input validation beyond what the domain factory already guards; it's picked up automatically by `ValidationPipelineBehavior` if registered.
- **Domain events**: `AggregateRoot` supports `Raise(IDomainEvent)` / `DomainEvents` / `ClearDomainEvents()`, and `EventDispatcherInterceptor` dispatches + clears them on EF `SaveChanges` — but **no aggregate currently calls `Raise(...)`**. This is present-but-unused infrastructure; follow it when a feature needs to react to a domain change, don't assume events are already flowing anywhere. Two more SharedKernel building blocks are in the same "present-but-unused" state: `Ardalis.GuardClauses` is globally imported in every `.Domain` project, but no `Create`/value-object constructor actually calls a guard clause; and `RowVersion` exists for optimistic concurrency, but no aggregate carries one today.
- **Naming/sealing conventions**: response records are `<Feature>CommandResponse` for commands (`CreateUserCommandResponse(Guid Id)`) and `<Aggregate>QueryResponse` for a single-item query (`UserQueryResponse`, `DeviceQueryResponse` — not `Get<Aggregate>ByIdResponse`); list items are `Get<Aggregate>ListItem`. GetOne query/request records keep the blended `Get<Aggregate>ByIdQueryCommand` suffix even though they implement `IQuery<T>` — match it, don't rename to `...Query`. Application-layer commands/queries/responses are always `sealed record`; Presentation-layer `[FromBody]` request DTOs (`NewUserModel`, `NewDeviceModel`) are plain `record`, not sealed. Handler classes are `internal sealed class`; **endpoint classes are `internal class`, never sealed** — consistent across every existing endpoint in both modules. When one concrete class implements several one-per-feature query interfaces, register it against **each** interface with its own `AddTransient<IInterface, Impl>()` call, not a single `AddTransient<Impl>()` with forwarding.
- **Strongly-typed IDs**: `record` wrappers (e.g. `DeviceId(string)`, `UserInternalId(Guid)`, `DeviceIntenralId(Guid)`) mapped to columns via EF value converters. Note the existing misspelling `DeviceIntenralId` is the real type name — match existing spelling when referencing it.
- **Database schema**: each module maps to its own Postgres schema (`HeadEnd` → `hes`, `UserAccess` → `ua`) via `HasDefaultSchema` + a per-schema migrations history table, both configured in the module's `InstallationExtensions`. `UseSnakeCaseNamingConvention()` maps PascalCase members to snake_case columns.
- **Endpoints**: implement `IEndpoint.MapEndpoint`, are instantiated and mapped in the module's `*InstallationExtensions.Map...Endpoints`, and grouped under a route prefix.
- **User context**: `InternalUserContextFactory` currently returns a `SYSTEM`/`ANONYMOUS` stub user (real auth is commented out / not yet wired).
- **Local variable typing**: declare a local as the narrowest interface its actual usage needs, not the first concrete type that happens to compile — e.g. `IReadOnlyCollection<T>` instead of `List<T>` when the variable is only ever handed to something expecting that interface. Collection expressions (`[.. ...]`) can target an interface directly since C# 12; the compiler picks the backing implementation, so narrowing costs nothing. Reference example: `UserQueryRepository`/`DeviceQueryRepository`/`ManufacturerQueryRepository`'s `GetListAsync` materialize Dapper's `IEnumerable<T>` result as `IReadOnlyCollection<TItem> list = [.. await res.ReadAsync<TItem>()];` before passing it to `ListQueryResponse<TData>`'s constructor — not `List<T>`.
- Each project has a `GlobalUsings.cs`; check it before adding `using` statements that may already be global. Notably: `.Domain` projects globally import `ErrorOr` and `Ardalis.GuardClauses`; `.Application` projects import `ErrorOr`, `Sergin.SharedKernel.Domain`, `Sergin.SharedKernel.Application`, and the module's own `.Domain`; `.Presentation.WebApi` projects import `ErrorOr`, `MediatR`, `Sergin.SharedKernel.Presentation*`; `.Infrastructure` projects import `Dapper` and `static Dapper.SqlMapper` (so raw `QuerySingleOrDefaultAsync` etc. are callable unqualified).

## SharedKernel

`src/SharedKernel/` holds framework-level building blocks shared across modules, mirroring the module layering: `.Domain` (`AggregateRoot`, `Entity`, guard clauses, `RowVersion`), `.Application` (command/query abstractions, pipeline behaviors, security, localization, time), `.Infrastructure` + `.Infrastructure.Data.EFCore` (`SerginDbContext` base, `IDbConnectionFactory` implementations, interceptors), and `.Presentation.WebApi` (`IEndpoint`, result mapping to ProblemDetails). Prefer extending these over duplicating primitives in a module.
