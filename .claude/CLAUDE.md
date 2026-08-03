# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Sergin is a .NET 10 **modular monolith** platform, built with DDD + Clean Architecture and per-feature vertical slices. It uses .NET Aspire for local orchestration and PostgreSQL for storage. There are currently two modules: **MeterMinder** — a Head-End System (HES) for smart electricity/gas/water meters (device communication, data collection) — and **UserAccess**, for identity and access.

**This repo (`Sergin.MeterMinder`) is the root/hostable repo of a three-repo split** — it's never itself embedded as someone else's submodule. `src/SharedKernel/` and `src/Modules/UserAccess/` are **git submodules** pointing at their own repos ([Sergin.SharedKernel](https://github.com/poursh/Sergin.SharedKernel), [Sergin.UserAccess](https://github.com/poursh/Sergin.UserAccess)) — changes to their code happen via PRs in those repos, not here. Clone with `git clone --recurse-submodules`, or run `git submodule update --init --recursive` after a plain clone (see Commands below). Each of the three repos carries its own `.claude/CLAUDE.md` scoped to what it owns; this file only covers what's specific to being the host (the `MeterMinder` module itself, the Host project, and how the pieces compose).

## Commands

Run all commands from the repo root. The solution uses the modern XML format (`Sergin.MeterMinder.slnx`); pass it explicitly or run from the repo root so the CLI resolves it automatically. Requires the .NET 10 SDK / VS 17.13+ / Rider.

```bash
# First-time clone (or after cloning without --recurse-submodules)
git submodule update --init --recursive

# Build (warnings are errors — see below)
dotnet build Sergin.MeterMinder.slnx

# Run the API directly (all-in-one host, Development profile applies EF migrations on startup)
dotnet run --project src/Hosts/Sergin.MeterMinder.Hosts.WebApi.All      # http://localhost:5000, Scalar UI at /scalar/v1

# Run everything in Docker (API + postgres:17 + Aspire dashboard)
docker compose -f docker-compose/docker-compose.yml up --build

# Run the integration test suite (needs Docker — spins up a real postgres:17 via Testcontainers)
dotnet test tests/Sergin.MeterMinder.IntegrationTests.WebApi.All/Sergin.MeterMinder.IntegrationTests.WebApi.All.csproj
```

`tests/Sergin.MeterMinder.IntegrationTests.WebApi.All` is the only test project so far — xUnit + `Testcontainers.PostgreSql` +
`Microsoft.AspNetCore.Mvc.Testing`, exercising the real `Sergin.MeterMinder.Hosts.WebApi.All` host end-to-end
(HTTP → command/query handler → EF write or raw-SQL read) against a disposable container rather than
mocks. There are no unit test projects yet.

**Test fixture pattern**: every test class shares one `SerginWebApiFactory<Program>` (`WebApplicationFactory<TEntryPoint>, IAsyncLifetime`,
generic over the host's entry point) via `[Collection(nameof(IntegrationTestCollection))]` — don't spin up a new factory
per test class. `SerginWebApiFactory<TEntryPoint>` lives in the `Sergin.SharedKernel.IntegrationTests` submodule project
(referenced here via `ProjectReference`, not owned by this repo) so any module's host can reuse it — it starts a
`Testcontainers.PostgreSql` container in `InitializeAsync` and sets the `Sergin__ConnectionStrings__Database` env var
*before* the host builds (a `ConfigureWebHost` override runs too late for this). Test classes live one folder per
aggregate (`tests/.../Users/CreateAndGetUserTests.cs`), inject `SerginWebApiFactory<Program>` via primary constructor,
and call `factory.CreateClient()` to hit real HTTP endpoints.

### EF Core migrations

Each module owns its own `DbContext` and migrations, so `--project` must point at that module's `Infrastructure.Data` project. `MeterMinderDbContext` and `UserAccessDbContext` each have an `IDesignTimeDbContextFactory` that reads the connection string from the `Sergin:ConnectionStrings:Database` key in `appsettings.Development.json`:

```bash
dotnet ef migrations add <Name> \
  --project src/Modules/MeterMinder/Sergin.MeterMinder.Infrastructure.Data \
  --startup-project src/Hosts/Sergin.MeterMinder.Hosts.WebApi.All

dotnet ef migrations add <Name> \
  --project src/Modules/UserAccess/Sergin.UserAccess.Infrastructure.Data \
  --startup-project src/Hosts/Sergin.MeterMinder.Hosts.WebApi.All
```

Migrations are applied automatically at startup **only in the Development environment** (the host bootstrap's `UseSerginWebApiAsync` in `Sergin.SharedKernel.Hosts.WebApi` calls every module's `ISerginModule.MigrateAsync`).

**Connection string sourcing**: the value isn't committed. The write side (both `DbContext`s), the read side (`IDbConnectionFactory`), and both design-time factories all read the same `Sergin:ConnectionStrings:Database` key. At runtime it comes from the `Sergin__ConnectionStrings__Database` environment variable (set in `docker-compose.yml`) or user secrets (the host declares a `UserSecretsId`) — `appsettings.json` carries only an empty placeholder and `appsettings.Development.json` carries none. **Gotcha**: the design-time factories load *only* `appsettings.Development.json` (not env vars or user secrets), so `dotnet ef` finds no connection string there unless you add the key to that file locally. `migrations add` scaffolds fine without one; `database update` from the CLI won't connect (startup auto-apply in Development is unaffected).

## Git conventions

- **Commit authorship**: Never add a `Co-Authored-By: Claude` trailer or otherwise attribute commits to Claude/the assistant. Commit under the user's configured git identity only.

## Critical build constraint

`Directory.Build.props` sets `TreatWarningsAsErrors=true`, `AnalysisMode=All`, and enables **SonarAnalyzer.CSharp** + `EnforceCodeStyleInBuild`. Any analyzer warning, style violation, or nullable warning **fails the build**. Nullable and implicit usings are enabled solution-wide. Write code that passes analysis cleanly the first time.

**Central Package Management is on.** `Directory.Packages.props` at the repo root sets `ManagePackageVersionsCentrally=true` and holds every package version as a `<PackageVersion>` entry. `PackageReference` items in the `.csproj` files (and the `SonarAnalyzer.CSharp` reference in `Directory.Build.props`) carry **no `Version` attribute** — a leftover version fails the build with NU1008. When adding a package to a project, reference it version-less (`<PackageReference Include="Foo" />`) and add/update its `<PackageVersion Include="Foo" Version="x.y.z" />` in `Directory.Packages.props`; keep that list alphabetical. The `Microsoft.Extensions.Options` transitive pin in `Sergin.SharedKernel.Hosts.WebApi` uses `PackageReference Update=` (also version-less) with its version centralized. `Directory.Packages.props` is registered in the `/solution-items/` folder of `Sergin.MeterMinder.slnx` alongside `Directory.Build.props`.

## Architecture

### Host / module composition

- **`Sergin.MeterMinder.Hosts.WebApi.All`** — the actual runnable Web API ("all-in-one" host). Its `Program.cs` is ~19 lines: it builds an `IReadOnlyCollection<ISerginModule>` (`[new MeterMinderModule(), new UserAccessModule()]`) and hands it to the WebApi bootstrap — `builder.AddSerginWebApi(modules)` before `Build()`, `await app.UseSerginWebApiAsync(modules)` after. Adding a module to a host = one `ProjectReference` + one element in that collection.
- **`Sergin.SharedKernel.Hosts`** — Aspire service defaults (OpenTelemetry, health checks, resilience, service discovery).
- **`Sergin.SharedKernel.Hosts.WebApi`** — Sergin-specific web bootstrap (`SerginWebApiExtensions`, namespace `Microsoft.Extensions.Hosting`): `AddSerginWebApi` registers MediatR (scanning every module's `ApplicationAssembly`) + pipeline behaviors, OpenAPI, event dispatcher/interceptor, `IDbConnectionFactory`, user context, localizer, then loops `module.AddServices(...)`; `UseSerginWebApiAsync` migrates every module (Development only), maps each `ISerginWebApiModule`'s endpoints under `MapGroup(module.Schema)`, then maps OpenAPI and (Development-only) Scalar.
- **Modules** live under `src/Modules/<ModuleName>/`: currently **`MeterMinder`** (schema `mm`) and **`UserAccess`** (schema `ua`). A module is wired into hosts through its **`<Module>Module` class** (in the `Sergin.<Module>` composition project, no suffix) implementing `ISerginWebApiModule` from `Sergin.SharedKernel.Modules`: `Schema`, `ApplicationAssembly`, `AddServices` (calls the generic `AddModuleDbContext<TContext, TIContext, TIUnitOfWork>` helper plus per-aggregate `Add<X>Dependencies()`), `MigrateAsync`, and `MapEndpoints` (per-aggregate `Map<X>Endpoints()`). One class per module implements all its capabilities; which capabilities run is the host's choice. Each module has its own `CLAUDE.md` (`src/Modules/<Module>/CLAUDE.md`) covering aggregate-specific details (implemented feature slices, quirks, unfinished pieces) that don't belong here.

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
- **`Sergin.<Module>`** (no suffix) — the module's composition root that references all the above and hosts the module's `<Module>Module` class.

### Adding a new feature

Use the **`/add-feature`** skill (`.claude/skills/add-feature/SKILL.md`) to scaffold a new CQRS vertical slice (command or query) — it encodes the full file-by-file layout (Application handler, Infrastructure repository wiring, Presentation endpoint, DI/route registration) following the UserAccess/Users reference pattern. Don't hand-roll the layout from memory; invoke the skill or read it for the authoritative shape.

### CQRS split

- **Writes**: endpoint → MediatR `ICommand` → `ICommandHandler` → domain `AggregateRoot` factory/behavior method → `IRepository` (EF Core) → `IUnitOfWork.SaveChangesAsync`. Each module has its own unit of work (e.g. `IMeterMinderUnitOfWork`, `IUserAccessUnitOfWork`), implemented by its `DbContext`.
- **Reads**: query handlers use dedicated query-repository interfaces (`I<Feature>QueryRepository`) backed by **raw SQL through `IDbConnectionFactory`** (Dapper-style `QuerySingleOrDefaultAsync` / `QueryMultipleAsync`), bypassing EF entirely for read models. A query handler maps a `null` result to `Error.NotFound()`.
  - Each query method opens its own `using DbConnection connection = await connectionFactory.CreateConnectionAsync();` — connections aren't shared or injected, one per call.
  - SQL is a raw `"""..."""` string literal; snake_case columns are aliased to match the response record's exact property casing so Dapper's binder matches (`SELECT user_name AS userName FROM ua.users WHERE id = @Id;`).
  - List queries batch **two** statements through one `QueryMultipleAsync` call — a `SELECT count(*) ...;` followed by the paged `SELECT ... LIMIT @PageSize OFFSET @Offset;` — then read them off the same `GridReader` (`ReadSingleAsync<int>()` then `ReadAsync<TItem>()`), wrapped as `new ListQueryResponse<TItem>(list, count)`. Both `UserQueryRepository` and `DeviceQueryRepository` use this exact shape.
  - The not-found idiom is **bare `Error.NotFound()`** with no custom code/description. Since `ApiProblemResults` localizes on `error.Code`, every not-found response across the API currently renders identical generic text regardless of aggregate — don't invent a per-feature `Error.NotFound(code, description)` without first checking the localization resources support it.

### CQRS structural gotchas

- **List-query features have no dedicated request record.** `Get<Aggregate>ListQueryCommandHandler` implements `IListQueryHandler<TItem>` directly against the shared generic `ListQuery<TItem>` (built by `ListQueryRequestModel.ToListQuery<TItem>()` in the endpoint) — there is no `Get<Aggregate>ListQueryCommand` type to attribute. This is *why* `[RequiredPermissions]` can't be applied to any `GetList` slice today — a structural gap in the shared generic type, not an inconsistently-applied convention. If a list feature needs authorization, that requires introducing a feature-specific list-query type first; there's no existing precedent for that shape, so flag it to the user rather than guessing.
- **`.Produces<TResponse>()` is called on Create/GetList endpoints but omitted on GetOne endpoints**, consistently in both modules. Match whichever family you're extending rather than "completing" the other.
- **Endpoint route strings never include the schema segment** (`/users`, not `/ua/users`) — the schema prefix is added exactly once by the host bootstrap (`app.MapGroup(module.Schema)` inside `UseSerginWebApiAsync`).
- **No FK-existence check on write.** `CreateDeviceCommandHandler` inserts a `Device` referencing `ManufacturerId` without checking the manufacturer exists — a bad ID surfaces as a raw Postgres FK-violation exception, not an `ErrorOr` result. This is the current state of the only cross-aggregate FK in the codebase, not an established pattern to replicate — no existing slice shows how to convert this into a friendly `ErrorOr` error.

### Cross-cutting conventions

- **Results**: handlers return `ErrorOr<T>` (the `ErrorOr` library, global-imported). Endpoints call `.ToApiResult()` to convert to an `IResult`/ProblemDetails.
- **MediatR pipeline behaviors** (registered in `Sergin.SharedKernel.Hosts.WebApi`'s `AddSerginWebApi`, order matters):
  1. `PermissionCheckPipelineBehavior` — enforces `[RequiredPermissionsAttribute]` on any `IBaseCommand` (covers both commands and queries) against `IUserContext`.
  2. `ValidationPipelineBehavior` — runs an optional FluentValidation `IValidator<TRequest>` if one is registered.
- **Permissions**: apply `[RequiredPermissions("permission.<schema>.<resource>.<action>")]` to a command/query record when it needs authorization, e.g. `"permission.ua.users.read"`, `"permission.mm.devices.read"`. This is opt-in per slice, not universally applied today — most commands have no attribute yet, so don't assume its absence on an existing handler is an oversight to fix incidentally.
- **Validation**: FluentValidation is wired but optional — no `AbstractValidator<T>` exists in the codebase yet. Add one alongside a command/query only when the feature actually needs input validation beyond what the domain factory already guards; it's picked up automatically by `ValidationPipelineBehavior` if registered.
- **Domain events**: `AggregateRoot` supports `Raise(IDomainEvent)` / `DomainEvents` / `ClearDomainEvents()`, and `EventDispatcherInterceptor` dispatches + clears them on EF `SaveChanges` — but **no aggregate currently calls `Raise(...)`**. This is present-but-unused infrastructure; follow it when a feature needs to react to a domain change, don't assume events are already flowing anywhere. Two more SharedKernel building blocks are in the same "present-but-unused" state: `Ardalis.GuardClauses` is globally imported in every `.Domain` project, but no `Create`/value-object constructor actually calls a guard clause; and `RowVersion` exists for optimistic concurrency, but no aggregate carries one today.
- **Naming/sealing conventions**: response records are `<Feature>CommandResponse` for commands (`CreateUserCommandResponse(Guid Id)`) and `<Aggregate>QueryResponse` for a single-item query (`UserQueryResponse`, `DeviceQueryResponse` — not `Get<Aggregate>ByIdResponse`); list items are `Get<Aggregate>ListItem`. GetOne query/request records keep the blended `Get<Aggregate>ByIdQueryCommand` suffix even though they implement `IQuery<T>` — match it, don't rename to `...Query`. Application-layer commands/queries/responses are always `sealed record`; Presentation-layer `[FromBody]` request DTOs (`NewUserModel`, `NewDeviceModel`) are plain `record`, not sealed. Handler classes are `internal sealed class`; **endpoint classes are `internal class`, never sealed** — consistent across every existing endpoint in both modules. When one concrete class implements several one-per-feature query interfaces, register it against **each** interface with its own `AddTransient<IInterface, Impl>()` call, not a single `AddTransient<Impl>()` with forwarding.
- **Strongly-typed IDs**: `record` wrappers (e.g. `DeviceId(string)`, `UserInternalId(Guid)`, `DeviceIntenralId(Guid)`) mapped to columns via EF value converters. Note the existing misspelling `DeviceIntenralId` is the real type name — match existing spelling when referencing it.
- **Database schema**: each module maps to its own Postgres schema (`MeterMinder` → `mm`, `UserAccess` → `ua`) via `HasDefaultSchema` (set in the module's `DbContext`) + a per-schema migrations history table (configured by the shared `AddModuleDbContext` helper that `<Module>Module.AddServices` calls). `UseSnakeCaseNamingConvention()` maps PascalCase members to snake_case columns.
- **Endpoints**: implement `IEndpoint.MapEndpoint`, are instantiated and mapped in the aggregate's `<Aggregate>InstallationExtensions.Map<Aggregate>Endpoints`, called from the module's `<Module>Module.MapEndpoints`, and grouped under a route prefix.
- **User context**: `InternalUserContextFactory` currently returns a `SYSTEM`/`ANONYMOUS` stub user (real auth is commented out / not yet wired).
- **Local variable typing**: declare a local as the narrowest interface its actual usage needs, not the first concrete type that happens to compile — e.g. `IReadOnlyCollection<T>` instead of `List<T>` when the variable is only ever handed to something expecting that interface. Collection expressions (`[.. ...]`) can target an interface directly since C# 12; the compiler picks the backing implementation, so narrowing costs nothing. Reference example: `UserQueryRepository`/`DeviceQueryRepository`/`ManufacturerQueryRepository`'s `GetListAsync` materialize Dapper's `IEnumerable<T>` result as `IReadOnlyCollection<TItem> list = [.. await res.ReadAsync<TItem>()];` before passing it to `ListQueryResponse<TData>`'s constructor — not `List<T>`.
- Each project has a `GlobalUsings.cs`; check it before adding `using` statements that may already be global. Notably: `.Domain` projects globally import `ErrorOr` and `Ardalis.GuardClauses`; `.Application` projects import `ErrorOr`, `Sergin.SharedKernel.Domain`, `Sergin.SharedKernel.Application`, and the module's own `.Domain`; `.Presentation.WebApi` projects import `ErrorOr`, `MediatR`, `Sergin.SharedKernel.Presentation*`; `.Infrastructure` projects import `Dapper` and `static Dapper.SqlMapper` (so raw `QuerySingleOrDefaultAsync` etc. are callable unqualified).

## SharedKernel and UserAccess are separate repos, mounted as submodules

- **`src/SharedKernel/`** ([Sergin.SharedKernel](https://github.com/poursh/Sergin.SharedKernel)) — framework-level building blocks shared across modules, mirroring the module layering: `.Domain` (`AggregateRoot`, `Entity`, guard clauses, `RowVersion`), `.Application` (command/query abstractions, pipeline behaviors, security, localization, time), `.Infrastructure` + `.Infrastructure.Data.EFCore` (`SerginDbContext` base, `IDbConnectionFactory` implementations, interceptors), and `.Presentation.WebApi` (`IEndpoint`, result mapping to ProblemDetails). Prefer extending these over duplicating primitives in a module. Fully standalone-buildable on its own (`dotnet build Sergin.SharedKernel.slnx` from inside that repo) — it has zero dependencies outside itself. See its own `.claude/CLAUDE.md` for the full reference.
- **`src/Modules/UserAccess/`** ([Sergin.UserAccess](https://github.com/poursh/Sergin.UserAccess)) — the UserAccess module. **Embed-only**: that repo deliberately has no solution file or `Directory.Build.props`/`Directory.Packages.props` of its own — it only compiles once mounted here (or in any other host that also provides a `Sergin.SharedKernel` submodule at a matching relative path). This is why `git submodule update --init --recursive` is required before `dotnet build Sergin.MeterMinder.slnx` works from a fresh clone. See its own `.claude/CLAUDE.md` for module-specific conventions.

Both are mounted at the *same relative paths* they occupied before the split (`src/SharedKernel/`, `src/Modules/UserAccess/`), which is what lets every `ProjectReference` in this repo and in UserAccess's own `.csproj` files resolve without any path rewrites — MSBuild's `Directory.Build.props`/`Directory.Packages.props` auto-discovery walks up the physical directory tree and doesn't care that a submodule boundary sits partway up.
