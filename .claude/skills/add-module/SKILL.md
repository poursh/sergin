---
name: add-module
description: Scaffold a brand-new module in the Sergin modular monolith — six projects (Domain, Application, Infrastructure, Infrastructure.Data, Presentation.WebApi, and the no-suffix composition root), their DbContext/schema/migrations wiring, solution-file entries, and host registration — following the existing UserAccess module as the template. Invoke with /add-module.
disable-model-invocation: false
---

Scaffold a new module for: $ARGUMENTS

Expected input: `<ModuleName> <SchemaName>`, e.g. `/add-module Billing bil`. Ask the user for whatever is missing — don't guess the module name or Postgres schema code. Schema codes in use so far: `mm` (MeterMinder), `ua` (UserAccess) — pick something short and distinct.

This is a much bigger, more error-prone scaffold than a single feature slice (see `/add-feature` for that). Use `src/Modules/UserAccess/**` as the reference implementation for every file below — read the matching file there before writing the new one, and match its style exactly (sealed/internal where UserAccess is sealed/internal, primary constructors, no comments). Do **not** add a first aggregate/feature as part of this skill — that's a separate `/add-feature` step once the module shell builds.

## 1. Create six projects under `src/Modules/<Module>/`

All are plain `Microsoft.NET.Sdk` (not `.Web`) class libraries — `Directory.Build.props` at the repo root already supplies `TargetFramework`, `Nullable`, analyzers, etc., so none of these csproj files need a `PropertyGroup`.

| Project | References | GlobalUsings.cs |
|---|---|---|
| `Sergin.<Module>.Domain` | `SharedKernel.Domain` | `global using ErrorOr;` / `global using Ardalis.GuardClauses;` |
| `Sergin.<Module>.Application` | `SharedKernel.Application`, `<Module>.Domain` | `global using ErrorOr;` / `Sergin.SharedKernel.Domain` / `Sergin.SharedKernel.Application` — **not** `Sergin.<Module>.Domain` yet (see note below) |
| `Sergin.<Module>.Infrastructure` | `SharedKernel.Infrastructure`, `<Module>.Application`, `<Module>.Infrastructure.Data` | `global using Dapper;` / `global using static Dapper.SqlMapper;` |
| `Sergin.<Module>.Infrastructure.Data` | `SharedKernel.Infrastructure.Data.EFCore`, `<Module>.Application` | (none needed yet — add if EF namespaces get noisy) |
| `Sergin.<Module>.Presentation.WebApi` | `SharedKernel.Presentation.WebApi`, `<Module>.Application` | `global using ErrorOr;` / `MediatR` / `Sergin.SharedKernel.Presentation` / `Sergin.SharedKernel.Presentation.WebApi` / `Sergin.SharedKernel.Presentation.WebApi.Endpoints` |
| `Sergin.<Module>` (composition root, no suffix) | `<Module>.Infrastructure`, `<Module>.Presentation.WebApi`, `SharedKernel.Modules` | (none) |

The composition root's csproj also needs:
```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```
(copy `Sergin.UserAccess.csproj` verbatim as the template — same three `ProjectReference`s + this `FrameworkReference`.)

**Note on the empty `Domain` project**: a C# namespace only exists once some type declares it, and this skill deliberately creates `Sergin.<Module>.Domain` with zero classes. Don't add `global using Sergin.<Module>.Domain;` to the Application project's `GlobalUsings.cs` yet — it won't compile — add that line as part of the first `/add-feature` invocation, once an aggregate under that namespace actually exists.

**`InternalsVisibleTo` — three places, not one.** `<Module>DbContext`, repositories, and endpoints are all `internal`, instantiated only from the composition root, so each of `.Infrastructure`, `.Infrastructure.Data`, and `.Presentation.WebApi` needs a `Properties/AssemblyInfo.cs` granting `[assembly: InternalsVisibleTo("Sergin.<Module>")]` (copy the UserAccess ones verbatim, swap the module name).

## 2. Application-layer plumbing (composition root of DI/MediatR)

In `Sergin.<Module>.Application/`:
- `<Module>AssemblyReference.cs` — note the actual class name is **`<Module>ApplicationAssemblyReference`** (matches `UserAccessApplicationAssemblyReference`, not just `UserAccessAssemblyReference`), wrapping `typeof(...).Assembly` for MediatR scanning.
- `I<Module>UnitOfWork.cs` — `public interface I<Module>UnitOfWork : IUnitOfWork;` (from `Sergin.SharedKernel.Application`).

## 3. Infrastructure.Data: DbContext, design-time factory, schema

In `Sergin.<Module>.Infrastructure.Data/`:
- `<Module>DbContext.cs` — mirror `UserAccessDbContext.cs`:
  ```csharp
  public interface I<Module>DbContext : IDbContext;

  internal sealed class <Module>DbContext(DbContextOptions<<Module>DbContext> options)
      : SerginDbContext(options), I<Module>DbContext, I<Module>UnitOfWork
  {
      public const string Schema = "<schema>";

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
          modelBuilder.HasDefaultSchema(Schema);
          modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
          base.OnModelCreating(modelBuilder);
      }
  }
  ```
- `<Module>DbContextDesignTimeFactory.cs` — copy `UserAccessDbContextDesignTimeFactory.cs`, swapping the type name and schema. This is what lets `dotnet ef migrations add` run against `appsettings.Development.json` without the host project.
- No `IEntityTypeConfiguration` / migration yet — those come from the first `/add-feature` slice, once there's an aggregate to map. An empty DbContext with no entities is fine as the initial scaffold; skip step 4 below (EF migration) until a feature adds a table.

## 4. Composition root: `Sergin.<Module>/<Module>Module.cs`

Create `Sergin.<Module>/<Module>Module.cs` — copy `Sergin.UserAccess/UserAccessModule.cs` exactly, renaming `UserAccess` → `<Module>` and swapping the schema/DbContext/assembly-reference types:

- `public sealed class <Module>Module : ISerginWebApiModule` (from `Sergin.SharedKernel.Modules`).
- `Schema` → `<Module>DbContext.Schema`; `ApplicationAssembly` → `<Module>ApplicationAssemblyReference.Assembly`.
- `AddServices` → `services.AddModuleDbContext<<Module>DbContext, I<Module>DbContext, I<Module>UnitOfWork>(configuration, <Module>DbContext.Schema);` plus per-aggregate `Add<X>Dependencies()` calls (none yet on a fresh module).
- `MigrateAsync` → `services.MigrateDbContextAsync<<Module>DbContext>();`
- `MapEndpoints` → per-aggregate `Map<X>Endpoints()` calls (empty method body on a fresh module).

Don't add an aggregate-specific `<Aggregate>InstallationExtensions.cs` (like `UserInstallationExtensions.cs`) as part of this skill — that's created by the first `/add-feature` invocation for this module.

## 5. Wire into the host

- `src/Hosts/Sergin.Hosts.WebApi.All/Sergin.Hosts.WebApi.All.csproj` — add `<ProjectReference Include="..\..\Modules\<Module>\Sergin.<Module>\Sergin.<Module>.csproj" />`.
- `src/Hosts/Sergin.Hosts.WebApi.All/Program.cs` — add `using Sergin.<Module>;` and one element to the modules collection: `IReadOnlyCollection<ISerginModule> modules = [new MeterMinderModule(), new UserAccessModule(), new <Module>Module()];` — nothing else; the bootstrap loops handle MediatR, DI, migrations, and endpoint mapping.

## 6. Register in `Sergin.slnx`

Add a new `<Folder Name="/src/Modules/<Module>/">` (mirroring the MeterMinder/UserAccess folders) listing the five non-Presentation projects, plus a `<Folder Name="/src/Modules/<Module>/Presentation/">` for the `.Presentation.WebApi` project alone — that split (Presentation project sits in its own subfolder) matches both existing modules.

## After scaffolding

1. Build to confirm the empty module compiles and wires up cleanly — this repo treats every analyzer/style warning as a build error:
   ```
   dotnet build Sergin.slnx
   ```
2. Run `dotnet run --project src/Hosts/Sergin.Hosts.WebApi.All` and confirm the app still starts (new module has no endpoints yet, so nothing new appears at `/scalar/v1`, but startup must not throw).
3. Hand off to `/add-feature <Module> <Aggregate> <Feature> command` for the module's first vertical slice — that step is what actually creates the aggregate, the `IEntityTypeConfiguration`, and the first EF migration.
