# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Sergin is a .NET 10 **modular monolith** for a HES (Head-End System) platform, built with DDD + Clean Architecture and per-feature vertical slices. It uses .NET Aspire for local orchestration and PostgreSQL for storage. There are currently two modules: **HeadEnd** and **UserAccess**.

## Commands

Run all commands from the repo root. The solution uses the modern XML format (`Sergin.slnx`); pass it explicitly or run from the repo root so the CLI resolves it automatically. Requires the .NET 10 SDK / VS 17.13+ / Rider.

```bash
# Build (warnings are errors — see below)
dotnet build Sergin.slnx

# Run the API directly (all-in-one host, Development profile applies EF migrations on startup)
dotnet run --project src/Hosts/Sergin.Hosts.WebApi.All      # http://localhost:5000, Scalar UI at /scalar/v1

# Run everything in Docker (API + postgres:17 + Aspire dashboard)
docker compose -f docker-compose/docker-compose.yml up --build
```

There are **no test projects** in the solution yet.

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
- **`.Application`** — MediatR commands/queries + handlers, `IUnitOfWork`, query repository interfaces. Feature folders hold the full slice under `<Aggregate>/Commands/<Feature>/...` — **queries live under `Commands/` too**, not a separate `Queries/` folder; don't invent one.
- **`.Infrastructure`** — write-side repositories (EF Core) and read-side query repositories (raw SQL via `IDbConnectionFactory`).
- **`.Infrastructure.Data`** — the module's `DbContext`, `IEntityTypeConfiguration`s, value converters, and migrations.
- **`.Presentation.WebApi`** — minimal-API endpoints implementing `IEndpoint`.
- **`Sergin.<Module>`** (no suffix) — the module's composition root that references all the above and exposes the installation extensions.

### Adding a new feature

Use the **`/add-feature`** skill (`.claude/skills/add-feature/SKILL.md`) to scaffold a new CQRS vertical slice (command or query) — it encodes the full file-by-file layout (Application handler, Infrastructure repository wiring, Presentation endpoint, DI/route registration) following the UserAccess/Users reference pattern. Don't hand-roll the layout from memory; invoke the skill or read it for the authoritative shape.

### CQRS split

- **Writes**: endpoint → MediatR `ICommand` → `ICommandHandler` → domain `AggregateRoot` factory/behavior method → `IRepository` (EF Core) → `IUnitOfWork.SaveChangesAsync`. Each module has its own unit of work (e.g. `IHeadEndUnitOfWork`, `IUserAccessUnitOfWork`), implemented by its `DbContext`.
- **Reads**: query handlers use dedicated query-repository interfaces (`I<Feature>QueryRepository`) backed by **raw SQL through `IDbConnectionFactory`** (Dapper-style `QuerySingleOrDefaultAsync` / `QueryMultipleAsync`), bypassing EF entirely for read models. A query handler maps a `null` result to `Error.NotFound()`.

### Cross-cutting conventions

- **Results**: handlers return `ErrorOr<T>` (the `ErrorOr` library, global-imported). Endpoints call `.ToApiResult()` to convert to an `IResult`/ProblemDetails.
- **MediatR pipeline behaviors** (registered in `Program.cs`, order matters):
  1. `PermissionCheckPipelineBehavior` — enforces `[RequiredPermissionsAttribute]` on any `IBaseCommand` (covers both commands and queries) against `IUserContext`.
  2. `ValidationPipelineBehavior` — runs an optional FluentValidation `IValidator<TRequest>` if one is registered.
- **Permissions**: apply `[RequiredPermissions("permission.<schema>.<resource>.<action>")]` to a command/query record when it needs authorization, e.g. `"permission.ua.users.read"`, `"permission.hes.devices.read"`. This is opt-in per slice, not universally applied today — most commands have no attribute yet, so don't assume its absence on an existing handler is an oversight to fix incidentally.
- **Validation**: FluentValidation is wired but optional — no `AbstractValidator<T>` exists in the codebase yet. Add one alongside a command/query only when the feature actually needs input validation beyond what the domain factory already guards; it's picked up automatically by `ValidationPipelineBehavior` if registered.
- **Domain events**: `AggregateRoot` supports `Raise(IDomainEvent)` / `DomainEvents` / `ClearDomainEvents()`, and `EventDispatcherInterceptor` dispatches + clears them on EF `SaveChanges` — but **no aggregate currently calls `Raise(...)`**. This is present-but-unused infrastructure; follow it when a feature needs to react to a domain change, don't assume events are already flowing anywhere.
- **Strongly-typed IDs**: `record` wrappers (e.g. `DeviceId(string)`, `UserInternalId(Guid)`, `DeviceIntenralId(Guid)`) mapped to columns via EF value converters. Note the existing misspelling `DeviceIntenralId` is the real type name — match existing spelling when referencing it.
- **Database schema**: each module maps to its own Postgres schema (`HeadEnd` → `hes`, `UserAccess` → `ua`) via `HasDefaultSchema` + a per-schema migrations history table, both configured in the module's `InstallationExtensions`. `UseSnakeCaseNamingConvention()` maps PascalCase members to snake_case columns.
- **Endpoints**: implement `IEndpoint.MapEndpoint`, are instantiated and mapped in the module's `*InstallationExtensions.Map...Endpoints`, and grouped under a route prefix.
- **User context**: `InternalUserContextFactory` currently returns a `SYSTEM`/`ANONYMOUS` stub user (real auth is commented out / not yet wired).
- Each project has a `GlobalUsings.cs`; check it before adding `using` statements that may already be global. Notably: `.Domain` projects globally import `ErrorOr` and `Ardalis.GuardClauses`; `.Application` projects import `ErrorOr`, `Sergin.SharedKernel.Domain`, `Sergin.SharedKernel.Application`, and the module's own `.Domain`; `.Presentation.WebApi` projects import `ErrorOr`, `MediatR`, `Sergin.SharedKernel.Presentation*`; `.Infrastructure` projects import `Dapper` and `static Dapper.SqlMapper` (so raw `QuerySingleOrDefaultAsync` etc. are callable unqualified).

## SharedKernel

`src/SharedKernel/` holds framework-level building blocks shared across modules, mirroring the module layering: `.Domain` (`AggregateRoot`, `Entity`, guard clauses, `RowVersion`), `.Application` (command/query abstractions, pipeline behaviors, security, localization, time), `.Infrastructure` + `.Infrastructure.Data.EFCore` (`SerginDbContext` base, `IDbConnectionFactory` implementations, interceptors), and `.Presentation.WebApi` (`IEndpoint`, result mapping to ProblemDetails). Prefer extending these over duplicating primitives in a module.
