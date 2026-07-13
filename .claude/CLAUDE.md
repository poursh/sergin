# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Sergin is a .NET 10 **modular monolith** for a HES (Head-End System) platform, built with DDD + Clean Architecture and per-feature vertical slices. It uses .NET Aspire for local orchestration and PostgreSQL for storage.

## Commands

Run all commands from the repo root. The solution uses the modern XML format (`Sergin.slnx`); pass it explicitly or run from the repo root so the CLI resolves it automatically. Requires the .NET 10 SDK / VS 17.13+ / Rider.

```bash
# Build (warnings are errors — see below)
dotnet build Sergin.slnx

# Run the API directly (all-in-one host, Development profile applies EF migrations on startup)
dotnet run --project src/Hosts/Sergin.Hosts.All      # http://localhost:5000, Scalar UI at /scalar/v1

# Run via .NET Aspire (spins up Postgres + pgAdmin + dashboard, then the API)
dotnet run --project Sergin.Host

# Run everything in Docker (API + postgres:17 + Aspire dashboard)
docker compose -f docker-compose/docker-compose.yml up --build
```

There are **no test projects** in the solution yet.

### EF Core migrations

Each module owns its own `DbContext` and migrations. The `HeadEndDbContext` has an `IDesignTimeDbContextFactory` that reads `appsettings.Development.json` for the connection string, so migration commands run against the `Infrastructure.Data` project:

```bash
dotnet ef migrations add <Name> \
  --project src/Modules/HeadEnd/Sergin.HeadEnd.Infrastructure.Data \
  --startup-project src/Hosts/Sergin.Hosts.All
```

Migrations are applied automatically at startup **only in the Development environment** (`RunHeadEndModule` → `ApplyMigration`).

## Git conventions

- **Commit authorship**: Never add a `Co-Authored-By: Claude` trailer or otherwise attribute commits to Claude/the assistant. Commit under the user's configured git identity only.

## Critical build constraint

`Directory.Build.props` sets `TreatWarningsAsErrors=true`, `AnalysisMode=All`, and enables **SonarAnalyzer.CSharp** + `EnforceCodeStyleInBuild`. Any analyzer warning, style violation, or nullable warning **fails the build**. Nullable and implicit usings are enabled solution-wide. Write code that passes analysis cleanly the first time.

## Architecture

### Host / module composition

- **`Sergin.Host`** — the .NET Aspire AppHost (orchestrator). Declares Postgres + the app resource; not the app itself.
- **`Sergin.Hosts.All`** — the actual runnable Web API ("all-in-one" host). Its `Program.cs` is the composition root: registers MediatR + pipeline behaviors, EF interceptors, `IDbConnectionFactory`, user context, localizer, and calls `builder.Services.AddHeadEndModule(...)` / `app.RunHeadEndModule()`.
- **`Sergin.Hosts.Shared`** — Aspire service defaults (OpenTelemetry, health checks).
- **Modules** live under `src/Modules/<ModuleName>/` (currently only `HeadEnd`). A module is wired into the host through its **`InstallationExtensions`** (in the `Sergin.HeadEnd` composition project): `AddHeadEndModule` registers DI + DbContext; `RunHeadEndModule` maps endpoints under a route group (`hes/...`) and applies migrations.

### Per-module project layering (see `HeadEnd`)

A module is split into projects that enforce Clean Architecture dependency direction:

- **`.Domain`** — aggregates/entities, strongly-typed IDs, repository interfaces. Depends only on `SharedKernel.Domain`.
- **`.Application`** — MediatR commands/queries + handlers, `IUnitOfWork`, query repository interfaces. Feature folders hold the full slice (`Devices/Commands/Create/...`).
- **`.Infrastructure`** — write-side repositories (EF Core) and read-side query repositories (raw SQL via `IDbConnectionFactory`).
- **`.Infrastructure.Data`** — the module's `DbContext`, `IEntityTypeConfiguration`s, value converters, and migrations.
- **`.Presentation`** — minimal-API endpoints implementing `IEndpoint`.
- **`Sergin.HeadEnd`** (no suffix) — the module's composition root that references all the above and exposes the installation extensions.

### CQRS split

- **Writes**: endpoint → MediatR `ICommand` → `ICommandHandler` → domain `AggregateRoot` factory method → `IRepository` (EF Core) → `IUnitOfWork.SaveChangesAsync`. Each module has its own unit of work (`IHeadEndUnitOfWork`), implemented by its `DbContext`.
- **Reads**: query handlers use dedicated query-repository interfaces backed by **raw SQL through `IDbConnectionFactory`** (Dapper-style `QuerySingleOrDefaultAsync` / `QueryMultipleAsync`), bypassing EF entirely for read models.

### Cross-cutting conventions

- **Results**: handlers return `ErrorOr<T>` (the `ErrorOr` library, global-imported). Endpoints call `.ToApiResult()` to convert to an `IResult`/ProblemDetails.
- **MediatR pipeline behaviors** (registered in `Program.cs`, order matters):
  1. `PermissionCheckPipelineBehavior` — enforces `[RequiredPermissions]` on commands against `IUserContext`.
  2. `ValidationPipelineBehavior` — runs an optional FluentValidation `IValidator<TRequest>` if one is registered.
- **Domain events**: raised on aggregate roots (`Raise(...)`), collected and dispatched by `EventDispatcherInterceptor` on EF `SaveChanges`, then cleared.
- **Strongly-typed IDs**: `record` wrappers (e.g. `DeviceId(string)`, `DeviceIntenralId(Guid)`) mapped to columns via EF value converters. Note the existing misspelling `DeviceIntenralId` is the real type name — match existing spelling when referencing it.
- **Database schema**: each module maps to its own Postgres schema (`HeadEnd` → `hes`) via `HasDefaultSchema` + a per-schema migrations history table. `UseSnakeCaseNamingConvention()` maps PascalCase members to snake_case columns.
- **Endpoints**: implement `IEndpoint.MapEndpoint`, are instantiated and mapped in the module's `*InstallationExtensions.Map...Endpoints`, and grouped under a route prefix.
- **User context**: `InternalUserContextFactory` currently returns a `SYSTEM`/`ANONYMOUS` stub user (real auth is commented out / not yet wired).
- Each project has a `GlobalUsings.cs`; check it before adding `using` statements that may already be global (e.g. `ErrorOr`, `Sergin.SharedKernel.*`).

## SharedKernel

`src/SharedKernel/` holds framework-level building blocks shared across modules, mirroring the module layering: `.Domain` (`AggregateRoot`, `Entity`, guard clauses, `RowVersion`), `.Application` (command/query abstractions, pipeline behaviors, security, localization, time), `.Infrastructure` + `.Infrastructure.Data.EFCore` (`SerginDbContext` base, `IDbConnectionFactory` implementations, interceptors), and `.Presentation.WebApi` (`IEndpoint`, result mapping to ProblemDetails). Prefer extending these over duplicating primitives in a module.
