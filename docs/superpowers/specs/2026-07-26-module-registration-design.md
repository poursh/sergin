# Module Registration Redesign — Design Spec

- **Date**: 2026-07-26
- **Status**: Approved (brainstorming dialogue, all sections signed off)
- **Goal**: Register modules into hosts through a uniform contract so the host project stays clean, adding a module touches one line of host code, and the duplicated installer boilerplate between modules disappears.

## Problem

Adding a module today touches `Sergin.Hosts.WebApi.All/Program.cs` in three places (`Register<Module>Commands()` inside `AddMediatR`, `Add<Module>Module(serginSection)`, `await app.Run<Module>Module()`) plus a `using` and a `ProjectReference`. Separately, each module's `InstallationExtensions.cs` duplicates near-identical bodies: `AddDbContextAndUnitOfWork` (UseNpgsql + per-schema migrations history table + snake_case + interceptor), `ApplyMigration`, and the `Run<Module>Module` shape (dev-only migrate → `MapGroup(schema)` → map endpoints). Both grow linearly with module count, and a future subset host would copy the entire plumbing block.

## Decisions made during brainstorming

1. **Scope: both layers** — host-side registration *and* the duplicated module-installer internals.
2. **Subset hosts are planned** (e.g. a future `WebApi.HeadEnd`), so host-level plumbing is hoisted into `Sergin.Hosts.Shared` now.
3. **Approach: explicit module contract + per-host list** (`ISerginModule[]` in Program.cs). Reflection auto-discovery was rejected: it saves one line per module but introduces silent-missing-module failures (the CLR doesn't load referenced-but-unused assemblies), nondeterministic ordering, and analyzer/AOT friction. A source-generator approach was rejected as overkill.
4. **Interface-only contract, no abstract base class.** A `SerginModule<TDbContext>` base cannot work: the DbContexts are `internal`, and a public module class cannot inherit a public generic base closed over an internal type argument (CS0060). With an interface, internal types appear only inside method bodies, which is legal, and the DbContexts stay internal.
5. **The contract lives in a new project `Sergin.SharedKernel.Modules`** containing only `ISerginModule`. The generic DbContext/migration helpers stay in `Sergin.SharedKernel.Infrastructure.Data.EFCore`, which already owns the EF/Npgsql references and the internal `EventDispatcherInterceptor` they use.
6. **`MigrateAsync` and `MapEndpoints` are separate contract members** (not one `RunAsync`): the module knows *how* to migrate; the host owns the *when* (dev-only policy) in exactly one place.
7. **Forwarding DbContext registrations** (approved behavioral improvement): `TIContext` and `TIUnitOfWork` resolve to the same scoped `TContext` instance instead of today's second instance + null-unsafe `as` cast. This is the only intentional behavior change in the design.

## Architecture

| Piece | Home | Content |
|---|---|---|
| `ISerginModule` | **new** `src/SharedKernel/Sergin.SharedKernel.Modules` | the 5-member contract |
| `AddModuleDbContext`, `MigrateDbContextAsync` | `Sergin.SharedKernel.Infrastructure.Data.EFCore` | generic helpers absorbing the duplicated installer bodies |
| `AddSerginWebApi`, `UseSerginWebApiAsync` | `Sergin.Hosts.Shared` | host bootstrap: shared services + module loops |
| `HeadEndModule`, `UserAccessModule` | each module's composition root (`Sergin.<Module>`) | one public sealed class per module, replacing the module-level `InstallationExtensions.cs` |

Per-aggregate extensions (`UserInstallationExtensions`, `DeviceInstallationExtensions`, `ManufacturerInstallationExtensions`) are untouched — the module class calls them.

## The contract

```csharp
namespace Sergin.SharedKernel.Modules;

public interface ISerginModule
{
    string Schema { get; }
    Assembly ApplicationAssembly { get; }
    void AddServices(IServiceCollection services, IConfigurationSection configuration);
    Task MigrateAsync(IServiceProvider services);
    void MapEndpoints(RouteGroupBuilder group);
}
```

`Sergin.SharedKernel.Modules.csproj` is a plain `Microsoft.NET.Sdk` class library with only `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (supplies `RouteGroupBuilder`, `IServiceCollection`, `IConfigurationSection`). No `PropertyGroup` — `Directory.Build.props` supplies everything. Register it in `Sergin.slnx` under the SharedKernel folder.

## Module implementations

```csharp
// src/Modules/HeadEnd/Sergin.HeadEnd/HeadEndModule.cs
public sealed class HeadEndModule : ISerginModule
{
    public string Schema => HeadEndDbContext.Schema;
    public Assembly ApplicationAssembly => HeadEndApplicationAssemblyReference.Assembly;

    public void AddServices(IServiceCollection services, IConfigurationSection configuration)
    {
        services.AddModuleDbContext<HeadEndDbContext, IHeadEndDbContext, IHeadEndUnitOfWork>(configuration, HeadEndDbContext.Schema);
        services.AddDeviceDependencies();
        services.AddManufacturerDependencies();
    }

    public Task MigrateAsync(IServiceProvider services) =>
        services.MigrateDbContextAsync<HeadEndDbContext>();

    public void MapEndpoints(RouteGroupBuilder group) =>
        group.MapDeviceEndpoints().MapManufacturerEndpoints();
}
```

`UserAccessModule` is identical in shape (`UserAccessDbContext`, `IUserAccessDbContext`, `IUserAccessUnitOfWork`, `AddUserDependencies()`, `MapUserEndpoints()`). Both module-level `InstallationExtensions.cs` files are **deleted** (`Register<Module>Commands`, `Add<Module>Module`, `Run<Module>Module`, and the private helpers). The `<Module>ApplicationAssemblyReference` classes stay.

Each composition root csproj adds a `ProjectReference` to `Sergin.SharedKernel.Modules`.

## SharedKernel EFCore helpers

```csharp
// Sergin.SharedKernel.Infrastructure.Data.EFCore
public static class ModuleDbContextExtensions
{
    public static IServiceCollection AddModuleDbContext<TContext, TIContext, TIUnitOfWork>(
        this IServiceCollection services, IConfigurationSection configuration, string schema)
        where TContext : SerginDbContext, TIContext, TIUnitOfWork
        where TIContext : class
        where TIUnitOfWork : class
    {
        string connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Sergin:ConnectionStrings:Database' is not configured.");

        services.AddDbContext<TContext>((sp, options) =>
            options.UseNpgsql(connectionString,
                    pgOptions => pgOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, schema))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<EventDispatcherInterceptor>()));

        services.AddScoped<TIContext>(sp => sp.GetRequiredService<TContext>());
        services.AddScoped<TIUnitOfWork>(sp => sp.GetRequiredService<TContext>());

        return services;
    }

    public static async Task MigrateDbContextAsync<TContext>(this IServiceProvider services)
        where TContext : DbContext
    {
        using IServiceScope scope = services.CreateScope();
        using TContext context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Database.MigrateAsync();
    }
}
```

Notes: the naked type-parameter constraints (`TContext : TIContext, TIUnitOfWork`) make wrong wiring a compile error. `EventDispatcherInterceptor` is internal to this same assembly, so no visibility change is needed for the helper itself. The inner `using` on the resolved context mirrors the current `ApplyMigration` shape.

## Host bootstrap (`Sergin.Hosts.Shared`)

One new file with two extensions. Namespace: whatever namespace the existing `AddServiceDefaults` extension file in this project declares (the Aspire convention — extensions land in a `Microsoft.Extensions.*` namespace so Program.cs needs no extra `using`); the implementation must read that file and match it exactly.

- `AddSerginWebApi(this WebApplicationBuilder builder, IReadOnlyCollection<ISerginModule> modules)` returning `WebApplicationBuilder`:
  1. `IConfigurationSection serginSection = builder.Configuration.GetRequiredSection("Sergin");`
  2. One `AddMediatR` call: `foreach` module → `RegisterServicesFromAssembly(module.ApplicationAssembly)`, then `AddOpenBehavior(typeof(PermissionCheckPipelineBehavior<,>))`, `AddOpenBehavior(typeof(ValidationPipelineBehavior<,>))` — same order as today.
  3. `AddOpenApi()`; `IEventDispatcher`/`DefaultEventDispatcher`; `EventDispatcherInterceptor`; `IDbConnectionFactory` → `new PostgresDbConnectionFactory(connectionString)` where `connectionString` is read once as `serginSection.GetConnectionString("Database") ?? throw new InvalidOperationException(...)` (fail-fast replaces today's `!` bang); `AddHttpContextAccessor()`; `IUserContextFactory` → `InternalUserContextFactory` + scoped `CreateUserContext()`; `ILocalizer` → `DefaultLocalizer` — otherwise verbatim moves from today's Program.cs.
  4. `foreach` module → `module.AddServices(builder.Services, serginSection);`
- `UseSerginWebApiAsync(this WebApplication app, IReadOnlyCollection<ISerginModule> modules)` returning `Task<WebApplication>` (mirrors today's `Run<Module>Module` shape):
  1. `foreach` module → if `app.Environment.IsDevelopment()` then `await module.MigrateAsync(app.Services)`; then `module.MapEndpoints(app.MapGroup(module.Schema));`
  2. `app.MapOpenApi();` and dev-only `app.MapScalarApiReference();` — same timing as today (after module endpoint mapping).

## Resulting Program.cs

```csharp
using Sergin.HeadEnd;
using Sergin.SharedKernel.Modules;
using Sergin.UserAccess;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("sergin-all");

ISerginModule[] modules = [new HeadEndModule(), new UserAccessModule()];

builder.AddSerginWebApi(modules);

WebApplication app = builder.Build();

await app.UseSerginWebApiAsync(modules);

await app.RunAsync();

public partial class Program;   // integration tests (WebApplicationFactory<Program>) depend on this
```

Adding a module to a host = one `ProjectReference` + one array element. A future subset host = this same file with a different array and service name.

## Reference and csproj changes

- **`Sergin.Hosts.Shared`** gains project references: `Sergin.SharedKernel.Modules`, `Sergin.SharedKernel.Application`, `Sergin.SharedKernel.Infrastructure` (event dispatcher, localizer), `Sergin.SharedKernel.Infrastracture.Data` (connection factory — note existing "Infrastracture" spelling), `Sergin.SharedKernel.Infrastracture.WebApi` (user context), `Sergin.SharedKernel.Infrastructure.Data.EFCore` (interceptor); and version-less package references: `MediatR`, `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`. All versions already exist in `Directory.Packages.props`; verify and add any missing entry alphabetically.
- **`Sergin.Hosts.WebApi.All`** drops the `Microsoft.AspNetCore.OpenApi` and `Scalar.AspNetCore` package references and the direct `Sergin.SharedKernel.Infrastracture.WebApi` project reference (now transitive via Hosts.Shared). The `NuGetAuditSuppress` for GHSA-v5pm-xwqc-g5wc moves with the OpenApi package to Hosts.Shared; if the build still surfaces the advisory transitively in the host, keep the suppression in both projects (the build decides). **Keeps**: `Microsoft.EntityFrameworkCore.Design` (needed by `dotnet ef --startup-project`), `UserSecretsId`, Docker properties, module references, Hosts.Shared reference.
- **Module composition roots** (`Sergin.HeadEnd.csproj`, `Sergin.UserAccess.csproj`) each add a `ProjectReference` to `Sergin.SharedKernel.Modules`.
- **InternalsVisibleTo**: add `Sergin.Hosts.Shared` to the allowlist in `src/SharedKernel/Sergin.SharedKernel.Infrastructure.Data.EFCore/Properties/AssemblyInfo.cs` (it now registers the internal `EventDispatcherInterceptor`). Remove the `Sergin.Hosts.WebApi.All` grant if present and nothing in the host still touches internals — the build verifies.
- **`Sergin.slnx`**: add `Sergin.SharedKernel.Modules` under the SharedKernel folder.

## File inventory

**New**: `Sergin.SharedKernel.Modules/{csproj, ISerginModule.cs}`, `Sergin.Hosts.Shared/SerginWebApiExtensions.cs`, `Sergin.HeadEnd/HeadEndModule.cs`, `Sergin.UserAccess/UserAccessModule.cs`, `Sergin.SharedKernel.Infrastructure.Data.EFCore/ModuleDbContextExtensions.cs`.

**Deleted**: `Sergin.HeadEnd/InstallationExtensions.cs`, `Sergin.UserAccess/InstallationExtensions.cs`.

**Modified**: `Sergin.Hosts.WebApi.All/{Program.cs, csproj}`, `Sergin.Hosts.Shared.csproj`, both composition-root csproj files, EFCore SharedKernel `AssemblyInfo.cs`, `Sergin.slnx`, `.claude/CLAUDE.md`, `.claude/skills/add-module/SKILL.md`, `.claude/skills/add-feature/SKILL.md`, and `Directory.Packages.props` only if a central version entry turns out to be missing (expected: none — MediatR, OpenApi, and Scalar are already referenced elsewhere).

## Error handling

- Missing `"Sergin"` section: `GetRequiredSection` throws at startup (unchanged).
- Missing connection string: both `AddModuleDbContext` and the `IDbConnectionFactory` registration in `AddSerginWebApi` throw `InvalidOperationException` with the full config key name at startup, replacing today's null-bangs (which would surface as null-refs inside Npgsql).
- Empty or duplicate module arrays are not guarded — the list is compile-time visible in a ~15-line Program.cs; YAGNI.

## Behavior parity

Preserved exactly: MediatR scanning set, pipeline behavior order (PermissionCheck → Validation), module processing order (array order: HeadEnd, UserAccess), dev-only migration policy, dev-only Scalar, OpenAPI mapping timing, route group prefixes, per-schema migration history tables. The **only** intentional change is the single-scoped-DbContext unification (decision 7).

## Testing and verification

No new tests. The existing integration suite (`tests/Sergin.IntegrationTests`) runs the real `Sergin.Hosts.WebApi.All` host end-to-end against Testcontainers and exercises endpoints in both modules — a module dropped from the array, missing MediatR registration, or failed migration makes it fail. Verification steps:

1. `dotnet build Sergin.slnx` — analyzer-clean (warnings are errors).
2. `dotnet test tests/Sergin.IntegrationTests/Sergin.IntegrationTests.csproj` — green.
3. `dotnet run --project src/Hosts/Sergin.Hosts.WebApi.All` — starts, `/scalar/v1` lists both modules' endpoints.

## Documentation and skills updates (part of this work)

- **Root `.claude/CLAUDE.md`**: rewrite the "Host / module composition" section (contract, module classes, bootstrap, one-line-per-module registration); update the migrations wording (`Run<Module>Module` → host bootstrap + `ISerginModule.MigrateAsync`).
- **`.claude/skills/add-module/SKILL.md`**: step 2 table gains the `SharedKernel.Modules` reference for the composition root; step 4 becomes "write `<Module>Module : ISerginModule`" instead of the three extension methods; step 5 becomes "csproj reference + one array element in Program.cs".
- **`.claude/skills/add-feature/SKILL.md`**: update wherever it wires `Add<X>Dependencies` / `Map<X>Endpoints` into `InstallationExtensions` to point at the module class instead.

## Out of scope / future notes

- **Background jobs** (investigated 2026-07-26; no job infrastructure exists in the codebase — build none now). Binding rules for when the first real job arrives:
  - Job registration becomes an **optional capability interface** (e.g. `ISerginJobsModule : ISerginModule` with `AddJobs(IServiceCollection, IConfigurationSection)`), and is **never done inside `AddServices`** — otherwise every host that loads the module runs its jobs, and a scaled-out web tier executes every job once per replica. Hosts opt in explicitly (e.g. `modules.OfType<ISerginJobsModule>()`).
  - **Single-migrator rule**: exactly one host per environment auto-applies migrations (today: the web host). A jobs host's bootstrap must not call `MigrateAsync` — two hosts migrating the same database concurrently is a race.
  - A future `Sergin.Hosts.Jobs` reuses `ISerginModule` and the Hosts.Shared bootstrap unchanged: it can remain a `WebApplication` (Aspire service defaults expose health checks over HTTP) and simply never calls `MapEndpoints`. Composition intent: run jobs in-process in the all-in-one host first (one extra opt-in call); split them into the dedicated host when real workloads (fleet readouts, alarm polling, long protocol sessions) or web scale-out arrive — pinning that host to one instance defers distributed-locking concerns.
  - The job library choice (`BackgroundService` vs Quartz vs Hangfire) is deferred along with the interface — `AddJobs` is DI-registration-shaped and library-agnostic.
- **Reflection discovery** stays rejected; revisit only if the module count makes the explicit array genuinely painful.
- The list-query `[RequiredPermissions]` structural gap and the FK-existence-check gap noted in `CLAUDE.md` are untouched by this design.
