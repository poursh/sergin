# Sergin

A .NET 10 **modular monolith** platform, built with Domain-Driven Design (DDD), Clean Architecture, and per-feature vertical slices. It uses .NET Aspire for local orchestration and PostgreSQL for storage.

The central component is the **MeterMinder** module — a Head-End System (HES) for smart electricity/gas/water meters, the primary entry point for IoT device communication, data processing, and integration with other subsystems — alongside a **UserAccess** module for identity and access concerns. Both are composed into a single runnable host.

This repo (`Sergin.MeterMinder`) is the root/hostable repo of a three-repo split — **`src/SharedKernel/`** and **`src/Modules/UserAccess/`** are git submodules pointing at their own repos, [Sergin.SharedKernel](https://github.com/poursh/Sergin.SharedKernel) and [Sergin.UserAccess](https://github.com/poursh/Sergin.UserAccess). See "Getting Started" below for the clone step this requires.

## 🏗 Architectural Approach

The solution follows modern architecture practices to keep domain logic clear and the system maintainable and scalable:

- **Domain-Driven Design (DDD)** – Rich domain model with aggregates, strongly-typed IDs, domain events, and clear boundaries.
- **Clean Architecture** – Strict dependency direction across `Domain → Application → Infrastructure / Presentation`.
- **Modular Monolith** – Independent, self-contained modules (`MeterMinder`, `UserAccess`) that can later be decomposed into services.
- **CQRS** – Writes flow through MediatR commands to EF Core repositories; reads use dedicated query repositories backed by raw SQL for performance.

## 🧱 Solution Structure

```
src/
├── Hosts/
│   └── Sergin.MeterMinder.Hosts.WebApi.All # Runnable all-in-one Web API (composition root)
├── Modules/
│   ├── MeterMinder/                # Head-End System (HES) for smart meters
│   └── UserAccess/             # Identity & access module (git submodule)
└── SharedKernel/                   # Framework-level building blocks (git submodule)
    ├── Sergin.SharedKernel.Hosts         # Aspire service defaults (OpenTelemetry, health checks)
    ├── Sergin.SharedKernel.Hosts.WebApi  # Sergin WebApi bootstrap (MediatR, DI, endpoints)
    └── ...                               # Other framework-level building blocks
```

Each module is split into `.Domain`, `.Application`, `.Infrastructure`, `.Infrastructure.Data` (DbContext + migrations), and `.Presentation.WebApi` (minimal-API endpoints), plus a composition project that wires it into the host. Each module owns its own `DbContext`, migrations, and PostgreSQL schema.

## 📌 Key Features

- **MeterMinder**, a Head-End System (HES) for smart meter device and data management, plus a **UserAccess** module for users and permissions.
- Clean separation between domain, application, and infrastructure layers, enforced by project dependencies.
- CQRS with MediatR pipeline behaviors for permission checks and validation.
- Domain events raised on aggregates and dispatched on `SaveChanges` via EF Core interceptors.
- Extensible design for adding future modules with minimal coupling.

## 🛠 Technologies & Libraries

- **.NET 10** – Core development framework
- **.NET Aspire** – Observability dashboard (via the `aspire-dashboard` container in Docker Compose)
- **Entity Framework Core** – ORM for the write side, migrations, and value converters
- **Dapper / raw SQL** – High-performance read-side query repositories via `IDbConnectionFactory`
- **PostgreSQL** – Relational database backend (per-module schemas)
- **MediatR** – In-process messaging for CQRS and decoupled communication
- **FluentValidation** – Strongly-typed, fluent request validation
- **ErrorOr** – Result/error modeling for handlers, mapped to ProblemDetails at the API edge

## 🚀 Getting Started

Requires the **.NET 10 SDK** (VS 17.13+ / Rider). Run all commands from the repo root.

```bash
# Clone with submodules (SharedKernel + UserAccess live in their own repos)
git clone --recurse-submodules https://github.com/poursh/Sergin.MeterMinder.git

# ...or, for an existing clone that didn't use --recurse-submodules:
git submodule update --init --recursive

# Build (warnings are treated as errors — analyzers + SonarAnalyzer enforced)
dotnet build Sergin.MeterMinder.slnx

# Run everything in Docker (API + postgres:17 + Aspire dashboard)
# NB: submodules must be initialized first (above) — the Docker build context
# copies the whole working tree, submodule content included.
docker compose -f docker-compose/docker-compose.yml up --build
```

### Run from Visual Studio

If you use **Visual Studio** (17.13+), open `Sergin.MeterMinder.slnx`, set **`docker-compose`**
(`docker-compose/docker-compose.dcproj`) as the startup project, and press **F5**.
Visual Studio builds the images and launches the full stack (API + `postgres:17` +
Aspire dashboard) via Docker Compose, then attaches the debugger to the API.

### EF Core migrations

Each module owns its own `DbContext` and migrations. Example for the MeterMinder module:

```bash
dotnet ef migrations add <Name> \
  --project src/Modules/MeterMinder/Sergin.MeterMinder.Infrastructure.Data \
  --startup-project src/Hosts/Sergin.MeterMinder.Hosts.WebApi.All
```

Migrations are applied automatically at startup **only in the Development environment**.

> **Note:** `Directory.Build.props` enables `TreatWarningsAsErrors`, `AnalysisMode=All`, and SonarAnalyzer with `EnforceCodeStyleInBuild`. Any analyzer, style, or nullable warning will fail the build.
